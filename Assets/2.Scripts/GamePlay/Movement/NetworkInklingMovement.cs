using DefineEnum;
using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEngine;
using UnityEngine.Animations.Rigging;

// InklingController를 건드리지 않고 Simple KCC 기반으로 Fusion 2 네트워크 이동을 처리하는 래퍼
[RequireComponent(typeof(InklingController))]
[RequireComponent(typeof(SimpleKCC))]
public class NetworkInklingMovement : NetworkBehaviour
{
    [Header("Class Reference")]
    [SerializeField] InklingRenderController _renderC;
    [SerializeField] InkTankController _inkTankC;
    [SerializeField] CharacterClothChanger _characterClothChanger;
    [SerializeField] Weapon _weapon;

    [Header("Weapon")]
    [SerializeField] float _inktankOffset;
    [SerializeField] Color[] _teamColors1;
    [SerializeField] Color[] _teamColors2;

    Color _inkColor;
    Color _enemyColor;

    [Header("Movement Settings")]
    [SerializeField] float _walkSpeed = 5f;
    [SerializeField] float _swimSpeed = 7.5f;   // 아군 잉크 위 스쿼드 속도
    [SerializeField] float _slowSpeed = 1f;     // 적 잉크 위 속도
    [SerializeField] float _jumpImpulse = 8f;
    [SerializeField] float _gravity = -20f;

    [Header("Inertia Settings")]
    [SerializeField] float _groundAccel = 20f;  // 지상 가속도 (높을수록 즉각 반응)
    [SerializeField] float _airAccel = 4f;       // 공중 가속도 (낮을수록 관성 강함)
    [SerializeField] float _swimAccel = 8f;      // 수영 가속도

    [Networked] private Vector3 _currentMoveVelocity { get; set; }

    SimpleKCC _kcc;
    InklingController _inklingController;

    [Header("Aim Setting")]
    [SerializeField] MultiAimConstraint _multiAC;
    [SerializeField] RigBuilder _rigBuilder;
    [SerializeField] GameObject _mouseTarget;

    MaterialPropertyBlock _inklingMPB;

    [Header("Camera Setting")]
    [SerializeField] GameObject _cameraRoot;

    [Networked] public NetworkBool _isSquid { get; set; }
    [Networked] public NetworkBool _isShooting { get; set; }
    [Networked] public NetworkBool _isGrounded { get; set; }
    [Networked] public NetworkBool _isMoving  { get; set; }
    [Networked] public NetworkBool _switchFoot { get; set; }
    [Networked] public NetworkBool _isMorphingSquid { get; set; }
    [Networked] public NetworkBool _isMorphingInkling { get; set; }
    [Networked] public NetworkBool _isSameColor { get; set; }
    [Networked] public NetworkBool _isOnPaint { get; set; }
    [Networked] public NetworkBool _isSwimming {  get; set; }

    // 이전 프레임 값 — 변경 감지용 (네트워크 동기화 불필요)
    bool _prevIsOnPaint;
    bool _prevIsSameColor;
    [Networked] public TickTimer _morphTimer { get; set; }
    [Networked] public Vector3 _camForward { get; set; }
    [Networked] public Vector3 _camRight { get; set; }
    [Networked] public Vector3 _aimTargetPosition { get; set; }
    [Networked] public int _inkIdx { get; set; }
    [Networked] public float _layerWeight { get; set; }
    [Networked] public float _targetWeight {  get; set; }

    GameObject _aimTargetObj;

    public override void Spawned()
    {
        _kcc = GetComponent<SimpleKCC>();
        _inklingController = GetComponent<InklingController>();
        _characterClothChanger.SetCustomization();

        AssignTeamColors();

        // InklingController 초기화 후 Update 루프는 KCC가 대신 처리
        _inklingController.InitCharacter("sam");
        _inklingController.enabled = false;
        _renderC.Init();
        _inkTankC.Init();
        _renderC.SetTeamColor(_inkColor);

        _kcc.SetGravity(_gravity);

        AddAimSource();

        if (HasInputAuthority)
        {
            _cameraRoot.SetActive(true);
        }
        else
        {
            _cameraRoot.SetActive(false);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input)) return;

        CheckPaintColor();
        _inkTankC.SetInkUIPos();
        _inktankOffset = _inkTankC.UpdateInktankOffset();

        // 호스트가 input에서 조준 위치를 읽어 [Networked] 상태에 기록 → 모든 클라이언트에 동기화
        _aimTargetPosition = input._aimTargetPosition;

        // 카메라 방향 기반 이동 방향 계산
        _camForward = new Vector3(input._cameraForwardRight.x, 0f, input._cameraForwardRight.y).normalized;
        _camRight = new Vector3(_camForward.z, 0f, -_camForward.x);

        Vector3 dir = _camForward * input._movementInput.z + _camRight * input._movementInput.x;
        dir.Normalize();

        _isShooting = input._isShootPressed;

        // 발사체 스폰 (StateAuthority만 실행, 쿨다운 체크)
        if (_isShooting && !_isSquid && HasStateAuthority)
        {
            _weapon.ShootProjectile(_aimTargetPosition, _inkColor);
        }

        // 캐릭터 변신 여부 bool을 이용해 확인
        if (input._isSquidPressed && !_isMorphingSquid && !_isSquid)
        {
            _isMorphingSquid = true;
            _isSquid = true;
            _morphTimer = TickTimer.CreateFromSeconds(Runner, 0.1f);
        }

        if (_isMorphingSquid && _morphTimer.Expired(Runner))
        {            
            _isMorphingSquid = false;
        }

        if (!input._isSquidPressed && !_isMorphingInkling && _isSquid)
        {
            _isMorphingInkling = true;
            _isSquid = false;
            _morphTimer = TickTimer.CreateFromSeconds(Runner, 0.1f);
        }

        if (_isMorphingInkling && _morphTimer.Expired(Runner))
        {
            _isMorphingInkling = false;
        }

        if (_isSquid && _isSameColor && _isOnPaint) _isSwimming = true;
        else _isSwimming = false;

        // 속도 결정 (InklingController 상태 참조)
        float speed = UpdateSpeed();
        

        // 점프 (float impulse)
        float jumpImpulse = 0f;
        if (input._isJumpPressed && _kcc.IsGrounded)
        {
            _switchFoot = !_switchFoot;
            jumpImpulse = _jumpImpulse;
        }

        // 회전
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = input._isShootPressed && !_isSquid
                ? Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_camForward), 20f * Runner.DeltaTime)
                : Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 15f * Runner.DeltaTime);
            _kcc.SetLookRotation(targetRot);
        }
        else if (_isShooting && dir.sqrMagnitude == 0)
        {
            Quaternion targetRot = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_camForward), 20f * Runner.DeltaTime);
            _kcc.SetLookRotation(targetRot);
        }

        // 상태별 가속도 선택
        float accel = _kcc.IsGrounded && !_isSwimming
            ? _groundAccel
            : _isSwimming ? _swimAccel : _airAccel;

        Vector3 targetVelocity = dir * speed;
        _currentMoveVelocity = Vector3.MoveTowards(_currentMoveVelocity, targetVelocity, accel * Runner.DeltaTime);

        // KCC 이동 (방향 * 속도, 점프 impulse는 float)
        _kcc.Move(_currentMoveVelocity, jumpImpulse);

        if (_isShooting)
        {
            _targetWeight = 1;
        }
        else
        {
            _targetWeight = 0;
        }

        _layerWeight = Mathf.MoveTowards(_layerWeight, _targetWeight, 8 * Runner.DeltaTime);

        _isGrounded = _kcc.IsGrounded;
        _isMoving = dir.magnitude > 0.01f;


    }

    public override void Render()
    {
        LogPaintStatusChange();

        var renderstate = new InklingRenderController.RenderState
        {
            isSquid = _isSquid,
            isMorphingSquid = _isMorphingSquid,
            isMorphingInkling = _isMorphingInkling,
            isGrounded = _isGrounded,
            isMoving = _isMoving,
            isShooting = _isShooting,
            switchFoot = _switchFoot,
            isSameColor = _isSameColor,
            layerWeight = _layerWeight,
            isSwimming = _isSwimming,
            isOnPaint = _isOnPaint,
            localMoveVelocity = GetAnimationMoveVelocity(),
            cameraAngleX = GetCameraAngle(),
            teamColor = _inkColor,
            inktankOffset = _inktankOffset,
            hasInputAuthority = HasInputAuthority,
        };

        _renderC.UpdateRender(renderstate);

        var inkstate = new InkTankController.InkTankState
        {
            isSquid = _isSquid,
            isSameColor = _isSameColor,
            isJumping = !_isGrounded,
            isSwimming = _isSwimming,
        };

        _inkTankC.UpdateInkStatus(inkstate);

        // 원격 플레이어의 조준 타겟을 동기화된 위치로 이동
        if (!HasInputAuthority && _aimTargetObj != null)
        {
            _aimTargetObj.transform.position = _aimTargetPosition;
        }
    }

    float UpdateSpeed()
    {
        float speed = 0;

        if (_isSwimming)
            speed = _swimSpeed;
        else if (!_isSwimming)
        {
            if (!_isOnPaint || (_isOnPaint && _isSameColor))
                speed = _walkSpeed;
            else if(_isOnPaint && !_isSameColor)
                speed = _slowSpeed;
        }

        return speed;
    }

    void AddAimSource()
    {
        var sourceObj = _multiAC.data.sourceObjects;

        GameObject go = Instantiate(_mouseTarget, transform);

        go.name = $"MouseTarget{this.name}";
        _aimTargetObj = go;

        if (HasInputAuthority)
        {
            // 입력 핸들러에 MouseTarget 등록 → 조준 위치를 NetworkInputData로 전송
            var inputHandler = GetComponent<CharacterInputHandler>();
            if (inputHandler != null)
                inputHandler.SetMouseTarget(go.GetComponent<MouseTarget>());
        }

        if (!HasInputAuthority)
        {
            go.GetComponent<MouseTarget>().enabled = false;
            go.transform.localPosition = Vector3.forward * 10f;
        }

        var newsource = new WeightedTransform(go.transform, 1);
        sourceObj.Add(newsource);

        _multiAC.data.sourceObjects = sourceObj;

        if (_rigBuilder != null)
        {
            _rigBuilder.Build();
        }
    }

    float GetCameraAngle()
    {
        if (Camera.main == null) return 0f;
        float a = Camera.main.transform.eulerAngles.x;
        return a > 180 ? a - 360 : a;
    }

    Vector3 GetAnimationMoveVelocity()
    {
        if (_kcc.RealSpeed < 0.01f)
            return default;

        var velocity = _kcc.RealVelocity;

        // We only care about X an Z directions.
        velocity.y = 0f;

        if (velocity.sqrMagnitude > 1f)
        {
            velocity.Normalize();
        }

        // Transform velocity vector to local space.
        return transform.InverseTransformVector(velocity);
    }

    void LogPaintStatusChange()
    {
        bool onPaint    = _isOnPaint;
        bool sameColor  = _isSameColor;

        if (onPaint == _prevIsOnPaint && sameColor == _prevIsSameColor) return;

        string who       = HasInputAuthority ? "[나]" : $"[원격:{Object.InputAuthority}]";
        string paintStr  = onPaint ? (sameColor ? "아군 잉크" : "적 잉크") : "잉크 없음";

        Debug.Log($"{who} {Object.name} 페인트 상태 변경 → {paintStr}  (isOnPaint={onPaint}, isSameColor={sameColor})");

        _prevIsOnPaint   = onPaint;
        _prevIsSameColor = sameColor;
    }

    // 게임(세션)당 딱 한 번만 뽑히는 공용 색상 인덱스 — 호스트만 값을 정하고 네트워크로 전파한다
    static bool s_teamColorIndexAssigned;
    static int s_teamColorIndex;

    void AssignTeamColors()
    {
        var networkPlayer = GetComponent<NetworkPlayer>();
        int index = networkPlayer != null ? networkPlayer.SpawnIndex : 0;

        if (HasStateAuthority)
        {
            // SpawnIndex == 1은 매치의 첫 스폰(=새 매치 시작)을 의미하므로 이때는 무조건 다시 뽑는다
            if (index == 1 || !s_teamColorIndexAssigned)
            {
                s_teamColorIndex = Random.Range(0, _teamColors1.Length);
                s_teamColorIndexAssigned = true;
            }

            _inkIdx = s_teamColorIndex;
        }

        int myIdx = index % 2;
        Debug.Log("Index : " + index);
        if (myIdx == 1)
        {
            _inkColor = _teamColors1[_inkIdx];
            _enemyColor = _teamColors2[_inkIdx];
        }
        else
        {
            _inkColor = _teamColors2[_inkIdx];
            _enemyColor = _teamColors1[_inkIdx];
        }

        Debug.Log(_inkIdx);
    }

    void CheckPaintColor()
    {
        if (_isGrounded)
        {
            Ray ray = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                WorldInkReceiver receiver = hit.collider.GetComponent<WorldInkReceiver>();

                if (receiver != null)
                {
                    Color color = receiver.CheckPaintColor(hit);

                    CheckFloorStatus(color);
                }
                else
                {
                    _isSameColor = false;
                    _isOnPaint = false;
                }
            }
        }
    }

    void CheckFloorStatus(Color col)
    {
        //알파값이 낮으면 잉크가 없는 곳 [cite: 22, 23]
        if (col.a < 0.1f)
        {
            _isSameColor = false;
            _isOnPaint = false;
            Debug.Log("NotOnPaint");
        }
        else
            _isOnPaint = true;


        float distToMyTeam = Mathf.Abs(col.r - _inkColor.r) + Mathf.Abs(col.g - _inkColor.g) + Mathf.Abs(col.b - _inkColor.b);

        float distToEnemyTeam = Mathf.Abs(col.r - _enemyColor.r) + Mathf.Abs(col.g - _enemyColor.g) + Mathf.Abs(col.b - _enemyColor.b);

        if (distToMyTeam < distToEnemyTeam && distToMyTeam < 0.5f)
        {
            _isSameColor = true;
            Debug.Log("우리 팀 구역입니다!");
        }
        else if (distToEnemyTeam < distToMyTeam && distToEnemyTeam < 0.5f)
        {
            _isSameColor = false;
            Debug.Log("상대 팀 구역입니다!");
        }
    }
}

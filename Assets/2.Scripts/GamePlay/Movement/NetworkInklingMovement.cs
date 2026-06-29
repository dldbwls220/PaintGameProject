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

    [Header("Weapon")]
    [SerializeField] NetworkObject _projectilePrefab;
    [SerializeField] Transform _inkRoot;
    [SerializeField] float _shootSpeed = 20f;
    [SerializeField] float _shootRate = 0.1f;
    [SerializeField] Color _inkColor = Color.cyan;
    [SerializeField] Color _enemyColor = Color.lightPink;

    [Networked] private TickTimer _shootTimer { get; set; }

    [Header("Movement Settings")]
    [SerializeField] float _walkSpeed = 5f;
    [SerializeField] float _swimSpeed = 7.5f;   // 아군 잉크 위 스쿼드 속도
    [SerializeField] float _slowSpeed = 1f;     // 적 잉크 위 속도
    [SerializeField] float _jumpImpulse = 8f;
    [SerializeField] float _gravity = -20f;

    SimpleKCC _kcc;
    InklingController _inklingController;

    [Header("Inkling Anim N Render Setting")]
    //[SerializeField] Animator[] _anim;
    [SerializeField] MultiAimConstraint _multiAC;
    [SerializeField] RigBuilder _rigBuilder;
    //[SerializeField] GameObject[] _modelObj;
    [SerializeField] GameObject _mouseTarget;
    //SkinnedMeshRenderer[] _inkingRender;
    //SkinnedMeshRenderer[] _halfRender;
    //SkinnedMeshRenderer[] _squidRender;
    MaterialPropertyBlock _inklingMPB;

    [Header("Camera Setting")]
    [SerializeField] GameObject _cameraRoot;
    //[SerializeField] float _maxAlpha;
    //[SerializeField] float _minAlpha;
    //[SerializeField] float _upperThreshold = 35f;
    //[SerializeField] float _lowerThreshold = -35f;

    [Networked] public NetworkBool _isSquid { get; set; }
    [Networked] public NetworkBool _isShooting { get; set; }
    [Networked] public NetworkBool _isGrounded { get; set; }
    [Networked] public NetworkBool _isMoving  { get; set; }
    [Networked] public NetworkBool _switchFoot { get; set; }
    [Networked] public NetworkBool _isMorphingSquid { get; set; }
    [Networked] public NetworkBool _isMorphingInkling { get; set; }
    [Networked] public NetworkBool _isSameColor { get; set; }
    [Networked] public NetworkBool _isOnPaint { get; set; }
    [Networked] public NetworkBool _isClimbing {  get; set; }

    // 이전 프레임 값 — 변경 감지용 (네트워크 동기화 불필요)
    bool _prevIsOnPaint;
    bool _prevIsSameColor;
    [Networked] public TickTimer _morphTimer { get; set; }
    [Networked] public Vector3 _camForward { get; set; }
    [Networked] public Vector3 _camRight { get; set; }
    [Networked] public Vector3 _aimTargetPosition { get; set; }
    [Networked] public float _layerWeight { get; set; }
    [Networked] public float _targetWeight {  get; set; }

    GameObject _aimTargetObj;

    public override void Spawned()
    {
        _kcc = GetComponent<SimpleKCC>();
        _inklingController = GetComponent<InklingController>();

        // InklingController 초기화 후 Update 루프는 KCC가 대신 처리
        _inklingController.InitCharacter("sam");
        _inklingController.enabled = false;
        _renderC.Init();
        _inkTankC.Init();
        //_inkingRender = _modelObj[(int)FormState.Inkling].GetComponentsInChildren<SkinnedMeshRenderer>();
        //_halfRender = _modelObj[(int)FormState.Half].GetComponentsInChildren<SkinnedMeshRenderer>();
        //_squidRender = _modelObj[(int)FormState.Squid].GetComponentsInChildren<SkinnedMeshRenderer>();
        //_inklingMPB = new MaterialPropertyBlock();

        _kcc.SetGravity(_gravity);

        //SwitchRender(FormState.Inkling, true);
        //SwitchRender(FormState.Half, false);
        //SwitchRender(FormState.Squid, false);

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

        // 호스트가 input에서 조준 위치를 읽어 [Networked] 상태에 기록 → 모든 클라이언트에 동기화
        _aimTargetPosition = input._aimTargetPosition;

        // 카메라 방향 기반 이동 방향 계산
        _camForward = new Vector3(input._cameraForwardRight.x, 0f, input._cameraForwardRight.y).normalized;
        _camRight = new Vector3(_camForward.z, 0f, -_camForward.x);

        Vector3 dir = _camForward * input._movementInput.z + _camRight * input._movementInput.x;
        dir.Normalize();

        _isShooting = input._isShootPressed;

        // 발사체 스폰 (StateAuthority만 실행, 쿨다운 체크)
        if (_isShooting && !_isSquid && _shootTimer.ExpiredOrNotRunning(Runner) && HasStateAuthority)
        {
            _shootTimer = TickTimer.CreateFromSeconds(Runner, _shootRate);

            if (_projectilePrefab != null && _inkRoot != null)
            {
                Vector3 shootDir = (_aimTargetPosition - _inkRoot.position).normalized;
                var obj = Runner.Spawn(_projectilePrefab, _inkRoot.position, Quaternion.LookRotation(shootDir), Object.InputAuthority);
                obj.GetComponent<NetworkInkProjectile>()?.Initialize(_inkRoot.position, shootDir * _shootSpeed, _inkColor);
            }
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

        // 속도 결정 (InklingController 상태 참조)
        float speed = _walkSpeed;
        //if (_inklingController._nowSquid && _inklingController._nowOnPaint && _inklingController._nowSameColor)
        //    speed = _swimSpeed;
        //else if (!_inklingController._nowSameColor)
        //    speed = _slowSpeed;

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
                : Quaternion.LookRotation(dir);
            _kcc.SetLookRotation(targetRot);
        }
        else if (_isShooting && dir.sqrMagnitude == 0)
        {
            Quaternion targetRot = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_camForward), 20f * Runner.DeltaTime);
            _kcc.SetLookRotation(targetRot);
        }

        // KCC 이동 (방향 * 속도, 점프 impulse는 float)
        _kcc.Move(dir * speed, jumpImpulse);

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

        CheckPaintColor();
        _inkTankC.SetInkUIPos();
    }

    public override void Render()
    {
        LogPaintStatusChange();

        //var moveVelocity = GetAnimationMoveVelocity();


        //_anim[(int)FormState.Inkling].SetBool("isSquid", _isSquid);
        //_anim[(int)FormState.Inkling].SetBool("isShooting", _isShooting);
        //_anim[(int)FormState.Inkling].SetBool("isGround", _isGrounded);
        //_anim[(int)FormState.Inkling].SetBool("isJumping", !_isGrounded);
        //_anim[(int)FormState.Inkling].SetBool("FootSwitch", _switchFoot);

        //_anim[(int)FormState.Half].SetBool("isSquid", _isSquid);

        //_anim[(int)FormState.Squid].SetBool("isGround", _isGrounded);
        //_anim[(int)FormState.Squid].SetBool("isJumping", !_isGrounded);

        //if (_isMorphingSquid)
        //{
        //    SwitchRender(FormState.Half, true);
        //    SwitchRender(FormState.Inkling, false);
        //    SwitchRender(FormState.Squid, false);

        //    _anim[(int)FormState.Inkling].SetInteger("AniState", (int)AniState.Morph_toSquid);
        //    _anim[(int)FormState.Half].SetInteger("AniState", (int)AniState.Morph_toSquid);
        //}
        //else if (_isSquid && !_isMorphingSquid)
        //{
        //    SwitchRender(FormState.Inkling, false);
        //    SwitchRender(FormState.Half, false);
        //    SwitchRender(FormState.Squid, true);
        //}

        //if(_isMorphingInkling)
        //{
        //    SwitchRender(FormState.Inkling, false);
        //    SwitchRender(FormState.Half, true);
        //    SwitchRender(FormState.Squid, false);
        //    _anim[(int)FormState.Half].SetTrigger("ToHuman");
        //    _anim[(int)FormState.Inkling].SetTrigger("ToHuman");
        //    _anim[(int)FormState.Inkling].SetInteger("AniState", (int)AniState.Morph_toHuman);
        //    _anim[(int)FormState.Half].SetInteger("AniState", (int)AniState.Morph_toHuman);
        //}
        //else if(!_isSquid && !_isMorphingInkling)
        //{
        //    SwitchRender(FormState.Inkling, true);
        //    SwitchRender(FormState.Half, false);
        //    SwitchRender(FormState.Squid, false);
        //}

        //if (_isGrounded)
        //{
        //    if (_isSquid && !_isMorphingSquid)
        //    {
        //        _anim[(int)FormState.Squid].SetInteger("AniState", _isMoving ? (int)AniState.Squid_Walk : (int)AniState.Squid_Idle);
        //    }
        //    else if (!_isMorphingSquid && !_isMorphingInkling)
        //    {
        //        _anim[(int)FormState.Inkling].SetInteger("AniState", _isMoving ? (int)AniState.Run : (int)AniState.Idle);
        //        _anim[(int)FormState.Half].SetInteger("AniState", _isMoving ? (int)AniState.Run : (int)AniState.Idle);

        //        _anim[(int)FormState.Inkling].SetFloat("RNL", moveVelocity.x);
        //        _anim[(int)FormState.Inkling].SetFloat("FNB", moveVelocity.z);

        //        _anim[(int)FormState.Half].SetFloat("RNL", moveVelocity.x);
        //        _anim[(int)FormState.Half].SetFloat("FNB", moveVelocity.z);
        //    }          
        //}
        //else
        //{
        //    _anim[(int)FormState.Inkling].SetInteger("AniState", (int)AniState.Jump);
        //}

        //_anim[(int)FormState.Inkling].SetLayerWeight(1, _layerWeight);
        //_multiAC.weight = _layerWeight;

        var renderstate = new InklingRenderController.RenderState
        {
            isSquid = _isSquid,
            isMorphingSquid = _isMorphingSquid,
            isMorphingInkling = _isMorphingInkling,
            isGrounded = _isGrounded,
            isMoving = _isMoving,
            isShooting = _isShooting,
            switchFoot = _switchFoot,
            layerWeight = _layerWeight,
            localMoveVelocity = GetAnimationMoveVelocity(),
            cameraAngleX = GetCameraAngle(),   // 아래 헬퍼
            hasInputAuthority = HasInputAuthority,
        };

        _renderC.UpdateRender(renderstate);

        var inkstate = new InkTankController.InkTankState
        {
            isSquid = _isSquid,
            isSameColor = _isSameColor,
            isJumping = !_isGrounded,
            isClimbing = _isClimbing,
        };

        _inkTankC.UpdateInkStatus(inkstate);

        // 원격 플레이어의 조준 타겟을 동기화된 위치로 이동
        if (!HasInputAuthority && _aimTargetObj != null)
        {
            _aimTargetObj.transform.position = _aimTargetPosition;
        }

        //if (HasInputAuthority && Camera.main != null)
        //{
        //    float angle = Camera.main.transform.eulerAngles.x;
        //    if (angle > 180) angle -= 360;
        //    AngleTransparency(angle);
        //}
    }

    //void SwitchRender(FormState state, bool isOn)
    //{
    //    switch (state)
    //    {
    //        case FormState.Inkling:
    //            for (int i = 0; i < _inkingRender.Length; i++)
    //            {
    //                _inkingRender[i].enabled = isOn;
    //            }

    //            //for (int i = 0; i < _inkTankRender.Length; i++)
    //            //{
    //            //    _inkTankRender[i].enabled = isOn;
    //            //}

    //            break;
    //        case FormState.Half:
    //            for (int i = 0; i < _halfRender.Length; i++)
    //            {
    //                _halfRender[i].enabled = isOn;
    //            }
    //            break;
    //        case FormState.Squid:
    //            for (int i = 0; i < _squidRender.Length; i++)
    //            {
    //                _squidRender[i].enabled = isOn;
    //            }
    //            break;
    //    }

    //}
    //void AngleTransparency(float angle)
    //{
    //    float alpha = 1;
    //    float dither = 0;

    //    if (angle > _upperThreshold)
    //    {
    //        float range = 70f - _upperThreshold;
    //        float progress = (angle - _upperThreshold) / range;
    //        alpha = Mathf.Lerp(_maxAlpha, _minAlpha, progress);
    //        dither = Mathf.Lerp(_minAlpha, _maxAlpha, progress);
    //    }
    //    else if (angle < _lowerThreshold)
    //    {
    //        float range = Mathf.Abs(-70f - _lowerThreshold);
    //        float progress = (_lowerThreshold - angle) / range;
    //        alpha = Mathf.Lerp(_maxAlpha, _minAlpha, progress);
    //        dither = Mathf.Lerp(_minAlpha, _maxAlpha, progress);
    //    }

    //    foreach (SkinnedMeshRenderer ren in _inkingRender)
    //    {
    //        if (ren.name.Contains("_TeamC"))
    //        {
    //            ren.GetPropertyBlock(_inklingMPB);
    //            _inklingMPB.SetColor("_BaseColor", new Color(1, 1, 1, alpha));
    //            ren.SetPropertyBlock(_inklingMPB);
    //        }
    //        else
    //        {
    //            ren.GetPropertyBlock(_inklingMPB);
    //            _inklingMPB.SetColor("_BaseColor", new Color(1, 1, 1, alpha));
    //            ren.SetPropertyBlock(_inklingMPB);
    //        }
    //    }

        //foreach (MeshRenderer ren in _inkTankRender)
        //{
        //    if (ren.name.Contains("M_BombLine") || ren.name.Contains("M_Glass") || ren.name.Contains("M_Ink"))
        //    {
        //        ren.GetPropertyBlock(_inklingMPB);
        //        _inklingMPB.SetFloat("_DitherAlpha", dither);
        //        ren.SetPropertyBlock(_inklingMPB);

        //        if (ren.name.Contains("M_Ink"))
        //        {
        //            ren.GetPropertyBlock(_inklingMPB);
        //            _inklingMPB.SetVector("_Offset", new Vector2(0, _inkOffset));
        //            ren.SetPropertyBlock(_inklingMPB);
        //        }
        //    }
        //    else
        //    {
        //        ren.GetPropertyBlock(_inklingMPB);
        //        _inklingMPB.SetColor("_BaseColor", new Color(1, 1, 1, alpha));
        //        ren.SetPropertyBlock(_inklingMPB);
        //    }
        //}
    //}

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

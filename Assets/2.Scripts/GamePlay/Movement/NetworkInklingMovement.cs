using Cinemachine;
using DefineEnum;
using DefineStructure;
using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using static PlayerSoundManager;

// InklingController를 건드리지 않고 Simple KCC 기반으로 Fusion 2 네트워크 이동을 처리하는 래퍼
[RequireComponent(typeof(SimpleKCC))]
public class NetworkInklingMovement : NetworkBehaviour
{
    [Header("Local UI (GameUIManager가 스폰 시 연결)")]
    [SerializeField] CrosshairUI crosshairUI;

    [Header("Class Reference")]
    [SerializeField] InklingRenderController _renderC;
    [SerializeField] CharacterClothChanger _characterClothChanger;
    [SerializeField] WeaponManager _weaponManager;
    [SerializeField] WallClimb _wallClimb;
    [SerializeField] Health _health;
    [SerializeField] Hitbox _hitbox;
    [SerializeField] CharacterSeperation _characterSeparation;
    [SerializeField] PlayerSoundManager _playerSoundManager;

    [Header("Hitbox Size (Squid Form)")]
    [SerializeField] float _squidHitboxRadius = 0.5f;
    [SerializeField] float _squidHitboxExtents = 0.3f;
    [SerializeField] Vector3 _squidHitboxOffset = new Vector3(0f, 0.35f, 0f);

    float _inklingHitboxRadius;
    float _inklingHitboxExtents;
    Vector3 _inklingHitboxOffset;

    [Header("Weapon")]
    [SerializeField] Transform _shootRoot;
    [SerializeField] float _inktankOffset;

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

    [Header("Respawning Time")]
    [SerializeField] float _respawningTime = 1f;
    [SerializeField] float _turnDuration = 0.4f; // 반바퀴 회전에 걸리는 시간 (_respawningTime보다 짧게)

    [Networked] private Vector3 _currentMoveVelocity { get; set; }

    SimpleKCC _kcc;
    InklingController _inklingController;

    // MovementInput에서 계산되어 FixedUpdateNetwork에서 사용되는 입력 파생 값 (틱 단위 임시 값, 네트워크 동기화 불필요)
    Vector3 _moveDirection;
    float _pendingJumpImpulse;
    float _climbAxis;
    float _sideAxis;

    [Header("Aim Setting")]
    [SerializeField] MultiAimConstraint _multiAC;
    [SerializeField] RigBuilder _rigBuilder;
    [SerializeField] GameObject _mouseTarget;
    [SerializeField] GameObject _inkRoot;

    MaterialPropertyBlock _inklingMPB;

    [Header("Camera N Audio Setting")]
    [SerializeField] GameObject _cameraRoot;
    [SerializeField] GameObject _audioListnerRoot;
    [SerializeField] GameObject _vCamera;

    [Header("Swim Wake FX")]
    [SerializeField] GameObject _swimWakeFX;
    [SerializeField] float _swimWakeInterval = 0.15f;
    [SerializeField] float _swimWakeFXLifetime = 1f;
    float _swimWakeTimer;

    [Header("Morph Ink FX")]
    [SerializeField] GameObject _morphFX;
    [SerializeField] Vector3 _morphFXOffset;
    [SerializeField] float _morphFXLifeTime = 1f;

    [Networked, OnChangedRender(nameof(OnCustomizationChanged))]
    PlayerCustomization _custom { get; set; }

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
    [Networked] public NetworkBool _isSlowed { get; set; }
    [Networked] public NetworkBool _isWallClimb { get; set; }
    [Networked] public NetworkBool _isAlive { get; set; }

    // 이전 프레임 값 — 변경 감지용 (네트워크 동기화 불필요)
    bool _prevIsOnPaint;
    bool _prevIsSameColor;

    bool _prevMorphingSquid, _prevMorphingInkling, _prevRespawing, _prevPendingRespawn;

    [Networked] public TickTimer _morphTimer { get; set; }
    [Networked] public TickTimer _respawningTimer { get; set; }
    [Networked] Quaternion _respawnStartRot { get; set; }
    [Networked] NetworkBool _wasRespawning { get; set; }
    [Networked] public Vector3 _camForward { get; set; }
    [Networked] public Vector3 _camRight { get; set; }
    [Networked] public Vector3 _aimTargetPosition { get; set; }
    [Networked] public int _teamIndex { get; set; }
    [Networked] public float _layerWeight { get; set; }
    [Networked] public float _targetWeight {  get; set; }

    GameObject _aimTargetObj;

    void LateUpdate()
    {
        if (!HasInputAuthority || Camera.main == null) return;
        _inkRoot.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        _audioListnerRoot.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);

        if (crosshairUI != null || GameManager._instance._introFinished)
        {
            var target = _aimTargetObj.transform.GetComponent<MouseTarget>();

            crosshairUI.FollowTarget(_aimTargetObj.transform);
            crosshairUI.DetectEnemy(target._eHit);
            crosshairUI.OpenCrosshair(target._hA);
        }

    }

    public override void Spawned()
    {
        _kcc = GetComponent<SimpleKCC>();

        if (HasInputAuthority)
        {
            // 본인 캐릭터는 네트워크 왕복을 기다릴 필요 없이 로컬에 있는 실제 선택값을 바로 입힌다
            PlayerCustomization myCustom = PlayerCustomizeManager.instance.Customization;
            _characterClothChanger.SetCustomization(myCustom);
            RPC_SubmitCustomization(myCustom);
        }
        else
        {
            _characterClothChanger.SetCustomization(_custom);
        }

        _inklingHitboxRadius = _hitbox.CapsuleRadius;
        _inklingHitboxExtents = _hitbox.CapsuleExtents;
        _inklingHitboxOffset = _hitbox.Offset;

        AssignTeamColors();

        _renderC.Init();
        _renderC.SetTeamColor(_inkColor);

        _health.ApplyColorToFX(_enemyColor);

        _kcc.SetGravity(_gravity);

        _weaponManager.InitWeapon();

        AddAimSource();

        _playerSoundManager.SetLoopSFX();

        _weaponManager.Init(_inkColor, _aimTargetObj.transform, 100, _shootRoot.transform);

        if (HasInputAuthority)
        {
            _cameraRoot.SetActive(true);
            _audioListnerRoot.SetActive(true);
            GameUIManager._instance.RegisterLocalPlayer(this);
        }
        else
        {
            _cameraRoot.SetActive(false);
            _audioListnerRoot.SetActive(false);
        }

    }

    public void SetCrosshairUI(CrosshairUI crosshair)
    {
        crosshairUI = crosshair;
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input)) return;

        _isAlive = _health._isAlive;

        CheckPaintColor();

        _inktankOffset = _weaponManager.UpdateInktankOffset();

        //캐릭터 느려짐 여부 확인 (MovementInput의 변신 입력 처리보다 먼저 계산되어야 함)
        if (_isOnPaint && !_isSameColor) _isSlowed = true;
        else _isSlowed = false;

        MovementInput(input);

        // 발사체 스폰 (StateAuthority만 실행, 쿨다운/단발-연사 체크는 Weapon 내부에서)
        // 단발 무기의 rising-edge 감지를 위해 버튼을 뗀 상태에서도 매 틱 호출해야 함
        if (!_isSquid && HasStateAuthority)
        {
            _weaponManager.Shoot(_isShooting);
        }

        if (_isSquid && _isSameColor && _isOnPaint) _isSwimming = true;
        else _isSwimming = false;

        // 속도 결정 (InklingController 상태 참조)
        float speed = UpdateSpeed();

        // 회전
        if (_health._nowRespawing)
        {
            if (!_wasRespawning)
            {
                _kcc.SetPosition(GameManager._instance.GetSpawnPoint(_teamIndex, 0, true).position, teleport: true);

                _kcc.SetLookRotation(_teamIndex == 1 ? Quaternion.Euler(0, 180, 0) : Quaternion.identity);

                _respawningTimer = TickTimer.CreateFromSeconds(Runner, _respawningTime);
                _respawnStartRot = transform.rotation;
            }

            float elapsed = _respawningTime - (_respawningTimer.RemainingTime(Runner) ?? 0f);
            float t = Mathf.Clamp01(elapsed / _turnDuration);
            Quaternion targetRot = Quaternion.Slerp(_respawnStartRot * Quaternion.Euler(0f, 180f, 0f), _respawnStartRot, t);
            _kcc.SetLookRotation(targetRot);
        }
        else if (_moveDirection.sqrMagnitude > 0.01f && !_isWallClimb)
        {
            Quaternion targetRot = _isShooting && !_isSquid
                ? Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_camForward), 20f * Runner.DeltaTime)
                : Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_moveDirection), 15f * Runner.DeltaTime);
            _kcc.SetLookRotation(targetRot);
        }
        else if (_isShooting && _moveDirection.sqrMagnitude == 0)
        {
            Quaternion targetRot = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_camForward), 20f * Runner.DeltaTime);
            _kcc.SetLookRotation(targetRot);
        }

        _wasRespawning = _health._nowRespawing;

        // 상태별 가속도 선택
        float accel = _kcc.IsGrounded && !_isSwimming
            ? _groundAccel
            : _isSwimming ? _swimAccel : _airAccel;

        Vector3 targetVelocity = _moveDirection * speed;

        if (_isSquid && _wallClimb.CheckWall(_inkColor, _enemyColor))
        {
            _isWallClimb = true;
            _kcc.ResetVelocity();
            targetVelocity = _wallClimb.ClimbingWall(_climbAxis, _sideAxis); // X/Z까지 완전히 덮어씀
            _currentMoveVelocity = targetVelocity; // 관성 없이 즉시 반영 → 원본처럼 스냅한 반응

            //_kcc.SetLookRotation(_camForward);
        }
        else
        {
            _isWallClimb = false;
            _currentMoveVelocity = Vector3.MoveTowards(_currentMoveVelocity, targetVelocity, accel * Runner.DeltaTime);

        }

        if(_isWallClimb) _kcc.SetGravity(0);
        else _kcc.SetGravity(_gravity);

        // 캐릭터간 밀어내기: _currentMoveVelocity(관성 상태)에는 누적하지 않고, 이번 틱의 실제 이동에만 더함
        Vector3 separationVelocity = (_characterSeparation != null && !_isWallClimb)
            ? _characterSeparation.GetPushVelocity(this)
            : Vector3.zero;

        // KCC 이동 (방향 * 속도, 점프 impulse는 float)
        _kcc.Move(_currentMoveVelocity + separationVelocity, _pendingJumpImpulse);

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
        _isMoving = _moveDirection.magnitude > 0.01f;

        _weaponManager.AutoRefill(_isShooting);
        _weaponManager.RefillInk();
       
    }


    void MovementInput(NetworkInputData input)
    {
        if (!_health._isAlive || _health._nowRespawing || !GameManager._instance._gameStart) return;

        // 호스트가 input에서 조준 위치를 읽어 [Networked] 상태에 기록 → 모든 클라이언트에 동기화
        _aimTargetPosition = input._aimTargetPosition;

        // 카메라 방향 기반 이동 방향 계산
        _camForward = new Vector3(input._cameraForwardRight.x, 0f, input._cameraForwardRight.y).normalized;
        _camRight = new Vector3(_camForward.z, 0f, -_camForward.x);

        _moveDirection = _camForward * input._movementInput.z + _camRight * input._movementInput.x;
        _moveDirection.Normalize();

        _isShooting = input._isShootPressed;

        _climbAxis = input._climbAxis;
        _sideAxis = input._sideAxis;

        if (_isSlowed) input._isSquidPressed = false;

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

        // 점프 (float impulse)
        _pendingJumpImpulse = 0f;
        if (input._isJumpPressed && _kcc.IsGrounded)
        {
            _switchFoot = !_switchFoot;
            _pendingJumpImpulse = _jumpImpulse;
        }
    }

    public override void Render()
    {
        LogPaintStatusChange();

        // 히트박스를 둘로 나눠 enabled로 토글하면 LagCompensation이 꺼진 히트박스를
        // 계속 반환하는 문제가 있어, 히트박스 하나를 폼에 맞게 크기만 조절한다.
        // 권한과 무관하게 매 프레임 실행되는 Render에서 복제된 Networked 값만으로 계산한다.
        bool isInklingForm = _isMorphingSquid ? true : _isMorphingInkling ? false : !_isSquid;
        _hitbox.CapsuleRadius = isInklingForm ? _inklingHitboxRadius : _squidHitboxRadius;
        _hitbox.CapsuleExtents = isInklingForm ? _inklingHitboxExtents : _squidHitboxExtents;
        _hitbox.Offset = isInklingForm ? _inklingHitboxOffset : _squidHitboxOffset;

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
            isSlowed = _isSlowed,
            isAlive = _isAlive,
            isWallClimb = _isWallClimb,
            isRespawning = _health._nowRespawing,
            localMoveVelocity = GetAnimationMoveVelocity(),
            cameraAngleX = GetCameraAngle(),
            teamColor = _inkColor,
            inktankOffset = _inktankOffset,
            hasInputAuthority = HasInputAuthority,
        };

        var soundstate = new PlayerSoundManager.SoundState
        {
            isSquid = _isSquid,
            isMorphingSquid = _isMorphingSquid,
            isMorphingInkling = _isMorphingInkling,
            isGrounded = _isGrounded,
            isMoving = _isMoving,
            isShooting = _isShooting,
            isSwimming = _isSwimming,
            isSlowed = _isSlowed,
            isAlive = _isAlive,
            isWallClimb = _isWallClimb,
            isRespawning = _health._nowRespawing,
            wasMorphingSquid = _prevMorphingSquid,
            wasMorphingInkling = _prevMorphingInkling,
            wasRespwaning = _prevRespawing,
            wasPendingRespawn = _prevPendingRespawn,
            hasInputAuthority = HasInputAuthority,
            health = _health,
        };

        if (_prevMorphingInkling && !_isMorphingInkling)
        {
            PlayMorphSplashEffect();
        }

        
        PlaySwimWakeEffect();

        _playerSoundManager.UpdateSound(soundstate);
        _renderC.UpdateRender(renderstate);
        BoolCheck();

        var inkstate = new WeaponManager.InkTankState
        {
            isSquid = _isSquid,
            isSameColor = _isSameColor,
            isJumping = !_isGrounded,
            isSwimming = _isSwimming,
        };

        _weaponManager.UpdateInkStatus(inkstate);
        _weaponManager.UpdateShootSound();
        _health.PlayDeadSplashEffect();
        _health.PlayHitSound();

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
            if (!_isSlowed)
                speed = _walkSpeed;
            else
                speed = _slowSpeed;
        }

        return speed;
    }

    void AddAimSource()
    {
        var sourceObj = _multiAC.data.sourceObjects;

        GameObject go = Instantiate(_mouseTarget);

        go.name = $"MouseTarget{this.name}";
        _aimTargetObj = go;

        if (HasInputAuthority)
        {
            // 입력 핸들러에 MouseTarget 등록 → 조준 위치를 NetworkInputData로 전송
            var inputHandler = GetComponent<CharacterInputHandler>();
            if (inputHandler != null)
                inputHandler.SetMouseTarget(_aimTargetObj.GetComponent<MouseTarget>());

            _aimTargetObj.GetComponent<MouseTarget>().InitObj(_inkRoot, _weaponManager._distance, _teamIndex);
        }

        if (!HasInputAuthority)
        {
            _aimTargetObj.GetComponent<MouseTarget>().enabled = false;
            _aimTargetObj.transform.localPosition = Vector3.forward * 10f;
        }

        var newsource = new WeightedTransform(_aimTargetObj.transform, 1);
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

    // 로컬 클라이언트가 CustomizationScene에서 고른 값을 StateAuthority(호스트)에 전달 → PlayerCustomization._custom에 반영되어 모두에게 동기화된다
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_SubmitCustomization(PlayerCustomization custom)
    {
        _custom = custom;
        GameManager._instance.SetPlayerCustomization(Object.InputAuthority, custom);
    }

    // _custom이 동기화되어 값이 바뀔 때 호출된다.
    void OnCustomizationChanged()
    {
        if (HasInputAuthority) return;

        _characterClothChanger.SetCustomization(_custom);

        _renderC.RefreshClothRenderers();
        _renderC.SetTeamColor(_inkColor);
    }

    // 팀/잉크 색상은 GameManager가 스폰 시점에 등록해 둔 PlayerData에서 그대로 받아와 적용한다
    void AssignTeamColors()
    {
        var networkPlayer = GetComponent<NetworkPlayer>();
        int index = networkPlayer != null ? networkPlayer.SpawnIndex : 0;

        _teamIndex = index % 2;

        if (GameManager._instance.PlayerData.TryGet(Object.InputAuthority, out var playerData))
        {
            _inkColor = playerData._teamColor;
            _enemyColor = playerData._enemyColor;
        }

        gameObject.layer = LayerMask.NameToLayer(_teamIndex == 1 ? "Team1" : "Team2");
        _kcc.SetColliderLayer(gameObject.layer);
    }

    void CheckPaintColor()
    {
        if (_isGrounded)
        {
            Ray ray = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                WorldInkZoneReceiver receiver = hit.collider.GetComponent<WorldInkZoneReceiver>();

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

    // 잠수 수영 중(모델이 꺼진 채 잉크 속을 이동하는 동안) 발밑에 잉크가 살짝 솟는 웨이크 이펙트를 주기적으로 스폰
    void PlaySwimWakeEffect()
    {
        bool submerged = _isAlive && _isSquid && _isMoving && (_isWallClimb || (_isSwimming && _isGrounded));

        if (!submerged)
        {
            _swimWakeTimer = 0f;
            return;
        }

        _swimWakeTimer -= Runner.DeltaTime;
        if (_swimWakeTimer > 0f) return;

        _swimWakeTimer = _swimWakeInterval;

        if (_swimWakeFX == null) return;

        GameObject fx = Instantiate(_swimWakeFX, transform.position, Quaternion.identity);

        ParticleSystem[] ps = fx.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem p in ps)
        {
            var main = p.main;
            main.startColor = _inkColor;
        }

        Destroy(fx, _swimWakeFXLifetime);
    }

    void PlayMorphSplashEffect()
    {
        if (!_isAlive) return;

        if(_morphFX == null) return;

        GameObject fx = Instantiate(_morphFX, transform.position + _morphFXOffset, Quaternion.identity);

        ParticleSystem[] ps = fx.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem p in ps)
        {
            var main = p.main;
            main.startColor = _inkColor;
        }

        Destroy(fx, _morphFXLifeTime);
    }


    void BoolCheck()
    {
        _prevMorphingSquid = _isMorphingSquid;
        _prevMorphingInkling = _isMorphingInkling;
        _prevRespawing = _health._nowRespawing;
        _prevPendingRespawn = _health._pendingRespawn;
    }
}

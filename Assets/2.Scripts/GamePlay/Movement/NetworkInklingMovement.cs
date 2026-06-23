using DefineEnum;
using Fusion;
using Fusion.Addons.SimpleKCC;
using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;

// InklingController를 건드리지 않고 Simple KCC 기반으로 Fusion 2 네트워크 이동을 처리하는 래퍼
[RequireComponent(typeof(InklingController))]
[RequireComponent(typeof(SimpleKCC))]
public class NetworkInklingMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float _walkSpeed = 5f;
    [SerializeField] float _swimSpeed = 7.5f;   // 아군 잉크 위 스쿼드 속도
    [SerializeField] float _slowSpeed = 1f;     // 적 잉크 위 속도
    [SerializeField] float _jumpImpulse = 8f;
    [SerializeField] float _gravity = -20f;

    SimpleKCC _kcc;
    InklingController _inklingController;

    [Header("Inkling Anim N Render Setting")]
    [SerializeField] Animator[] _anim;
    [SerializeField] MultiAimConstraint _multiAC;
    [SerializeField] RigBuilder _rigBuilder;
    [SerializeField] GameObject[] _modelObj;
    [SerializeField] GameObject _mouseTarget;
    SkinnedMeshRenderer[] _inkingRender;
    SkinnedMeshRenderer[] _halfRender;
    SkinnedMeshRenderer[] _squidRender;
    MaterialPropertyBlock _inklingMPB;

    [Header("Camera Setting")]
    [SerializeField] GameObject _cameraRoot;
    [SerializeField] float _maxAlpha;
    [SerializeField] float _minAlpha;
    [SerializeField] float _upperThreshold = 35f;
    [SerializeField] float _lowerThreshold = -35f;

    [Networked] public NetworkBool _isSquid { get; set; }
    [Networked] public NetworkBool _isShooting { get; set; }
    [Networked] public NetworkBool _isGrounded { get; set; }
    [Networked] public NetworkBool _isMoving  { get; set; }
    [Networked] public NetworkBool _switchFoot { get; set; }
    [Networked] public float _layerWeight { get; set; }
    [Networked] public float _targetWeight {  get; set; }

    public override void Spawned()
    {
        _kcc = GetComponent<SimpleKCC>();
        _inklingController = GetComponent<InklingController>();

        // InklingController 초기화 후 Update 루프는 KCC가 대신 처리
        _inklingController.InitCharacter("sam");
        _inklingController.enabled = false;

        _inkingRender = _modelObj[(int)FormState.Inkling].GetComponentsInChildren<SkinnedMeshRenderer>();
        _halfRender = _modelObj[(int)FormState.Half].GetComponentsInChildren<SkinnedMeshRenderer>();
        _squidRender = _modelObj[(int)FormState.Squid].GetComponentsInChildren<SkinnedMeshRenderer>();
        _inklingMPB = new MaterialPropertyBlock();

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

        // 카메라 방향 기반 이동 방향 계산
        Vector3 camForward = new Vector3(input._cameraForwardRight.x, 0f, input._cameraForwardRight.y).normalized;
        Vector3 camRight = new Vector3(camForward.z, 0f, -camForward.x);

        Vector3 dir = camForward * input._movementInput.z + camRight * input._movementInput.x;
        dir.Normalize();

        // 네트워크 상태 업데이트
        _isSquid = input._isSquidPressed;
        _isShooting = input._isShootPressed;

        // 속도 결정 (InklingController 상태 참조)
        float speed = _walkSpeed;
        if (_inklingController._nowSquid && _inklingController._nowOnPaint && _inklingController._nowSameColor)
            speed = _swimSpeed;
        else if (!_inklingController._nowSameColor)
            speed = _slowSpeed;

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
            Quaternion targetRot = input._isShootPressed
                ? Quaternion.LookRotation(camForward)
                : Quaternion.LookRotation(dir);
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
    }

    public override void Render()
    {
        var moveVelocity = GetAnimationMoveVelocity();

        if (_isGrounded)
        {
            _anim[(int)FormState.Inkling].SetBool("isGround", _isGrounded);
            _anim[(int)FormState.Inkling].SetBool("isJumping", !_isGrounded);            
            _anim[(int)FormState.Inkling].SetInteger("AniState", _isMoving ? (int)AniState.Run : (int)AniState.Idle);

            _anim[(int)FormState.Inkling].SetFloat("RNL", moveVelocity.x);
            _anim[(int)FormState.Inkling].SetFloat("FNB", moveVelocity.z);

            _anim[(int)FormState.Half].SetFloat("RNL", moveVelocity.x);
            _anim[(int)FormState.Half].SetFloat("FNB", moveVelocity.z);
        }
        else
        {
            _anim[(int)FormState.Inkling].SetBool("isGround", _isGrounded);
            _anim[(int)FormState.Inkling].SetBool("isJumping", !_isGrounded);
            _anim[(int)FormState.Inkling].SetBool("FootSwitch", _switchFoot);
            _anim[(int)FormState.Inkling].SetInteger("AniState", (int)AniState.Jump);
        }

        _anim[(int)FormState.Inkling].SetBool("isShooting", _isShooting);
        _anim[(int)FormState.Inkling].SetLayerWeight(1, _layerWeight);
        _multiAC.weight = _layerWeight;

        if (HasInputAuthority && Camera.main != null)
        {
            float angle = Camera.main.transform.eulerAngles.x;
            if (angle > 180) angle -= 360;
            AngleTransparency(angle);
        }
    }


    void AngleTransparency(float angle)
    {
        float alpha = 1;
        float dither = 0;

        if (angle > _upperThreshold)
        {
            float range = 70f - _upperThreshold;
            float progress = (angle - _upperThreshold) / range;
            alpha = Mathf.Lerp(_maxAlpha, _minAlpha, progress);
            dither = Mathf.Lerp(_minAlpha, _maxAlpha, progress);
        }
        else if (angle < _lowerThreshold)
        {
            float range = Mathf.Abs(-70f - _lowerThreshold);
            float progress = (_lowerThreshold - angle) / range;
            alpha = Mathf.Lerp(_maxAlpha, _minAlpha, progress);
            dither = Mathf.Lerp(_minAlpha, _maxAlpha, progress);
        }

        foreach (SkinnedMeshRenderer ren in _inkingRender)
        {
            if (ren.name.Contains("_TeamC"))
            {
                ren.GetPropertyBlock(_inklingMPB);
                _inklingMPB.SetColor("_BaseColor", new Color(1, 1, 1, alpha));
                ren.SetPropertyBlock(_inklingMPB);
            }
            else
            {
                ren.GetPropertyBlock(_inklingMPB);
                _inklingMPB.SetColor("_BaseColor", new Color(1, 1, 1, alpha));
                ren.SetPropertyBlock(_inklingMPB);
            }
        }

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
    }

    void AddAimSource()
    {
        var sourceObj = _multiAC.data.sourceObjects;

        GameObject go = Instantiate(_mouseTarget, transform);

        go.name = $"MouseTarget{this.name}";

        var newsource = new WeightedTransform(go.transform, 1);
        sourceObj.Add(newsource);

        _multiAC.data.sourceObjects = sourceObj;

        if (_rigBuilder != null)
        {
            _rigBuilder.Build();
        }
    }

    private Vector3 GetAnimationMoveVelocity()
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
}

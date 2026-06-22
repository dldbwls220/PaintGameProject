using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEngine;

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

    [Networked] public NetworkBool IsSquid { get; set; }
    [Networked] public NetworkBool IsShooting { get; set; }

    public override void Spawned()
    {
        _kcc = GetComponent<SimpleKCC>();
        _inklingController = GetComponent<InklingController>();

        // InklingController 초기화 후 Update 루프는 KCC가 대신 처리
        _inklingController.InitCharacter("sam");
        _inklingController.enabled = false;

        _kcc.SetGravity(_gravity);
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input)) return;

        // 카메라 방향 기반 이동 방향 계산
        Vector3 camForward = new Vector3(input._cameraForwardRight.x, 0f, input._cameraForwardRight.y).normalized;
        Vector3 camRight = new Vector3(camForward.z, 0f, -camForward.x);

        Vector3 dir = camForward * input._movementInput.z + camRight * input._movementInput.x;

        // 네트워크 상태 업데이트
        IsSquid = input._isSquidPressed;
        IsShooting = input._isShootPressed;

        // 속도 결정 (InklingController 상태 참조)
        float speed = _walkSpeed;
        if (_inklingController._nowSquid && _inklingController._nowOnPaint && _inklingController._nowSameColor)
            speed = _swimSpeed;
        else if (!_inklingController._nowSameColor)
            speed = _slowSpeed;

        // 점프 (float impulse)
        float jumpImpulse = 0f;
        if (input._isJumpPressed && _kcc.IsGrounded)
            jumpImpulse = _jumpImpulse;

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
    }

    public override void Render()
    {
        // 원격 플레이어 시각 보간은 SimpleKCC가 자동 처리
    }
}

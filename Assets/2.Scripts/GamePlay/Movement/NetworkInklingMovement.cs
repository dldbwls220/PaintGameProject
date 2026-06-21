using Fusion;
using UnityEngine;

// InklingController를 건드리지 않고 Fusion 2 네트워크 이동을 처리하는 래퍼
[RequireComponent(typeof(InklingController))]
[RequireComponent(typeof(NetworkInklingController))]
public class NetworkInklingMovement : NetworkBehaviour
{
    InklingController _inklingController;
    NetworkInklingController _networkCC;

    [Networked] public NetworkBool IsSquid { get; set; }
    [Networked] public NetworkBool IsShooting { get; set; }

    public override void Spawned()
    {
        _inklingController = GetComponent<InklingController>();
        _networkCC = GetComponent<NetworkInklingController>();

        if (Object.HasInputAuthority)
        {
            // 로컬 플레이어는 InklingController의 일반 Update가 돌지 않도록
            // CharacterController는 NetworkInklingController가 제어
            _inklingController.enabled = false;
            _inklingController.InitCharacter("sam");
            _inklingController.enabled = false; // Init 후 다시 비활성 유지
        }
        else
        {
            // 원격 플레이어는 입력 없이 시각만 동기화
            _inklingController.enabled = false;
            _inklingController.InitCharacter("sam");
            _inklingController.enabled = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input)) return;

        // 카메라 forward/right 복원
        Vector3 camForward = new Vector3(input._cameraForwardRight.x, 0, input._cameraForwardRight.y);
        Vector3 camRight = new Vector3(camForward.z, 0, -camForward.x); // 90도 회전

        Vector3 dir = camForward * input._movementInput.z + camRight * input._movementInput.x;

        // 점프
        if (input._isJumpPressed)
            _networkCC.Jump();

        // 이동
        _networkCC.Move(dir);

        // 네트워크 상태 동기화 (애니메이션/외형용)
        IsShooting = input._isShootPressed;
        IsSquid = input._isSquidPressed;
    }

    public override void Render()
    {
        // 네트워크 상태를 InklingController 시각 로직에 반영
        // (애니메이션, 모델 전환 등은 InklingController 내부 메서드 활용)
    }
}

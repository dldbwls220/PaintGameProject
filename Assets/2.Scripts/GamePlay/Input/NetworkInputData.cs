using UnityEngine;
using Fusion;

public struct NetworkInputData : INetworkInput
{
    public Vector3 _movementInput;
    public float _rotationInput;
    public float _climbAxis;
    public float _sideAxis;
    public NetworkBool _isJumpPressed;
    public NetworkBool _isShootPressed;
    public NetworkBool _isSquidPressed;
    public Vector2 _cameraForward;      // 카메라 수평 forward (x, z). right는 서버에서 forward로 계산
    public Vector3 _aimTargetPosition;  // 마우스 조준 위치
}

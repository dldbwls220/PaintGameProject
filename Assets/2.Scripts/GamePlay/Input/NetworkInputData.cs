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
    public Vector2 _cameraForwardRight; // 카메라 기준 이동 방향 계산용
    public Vector3 _aimTargetPosition;  // 마우스 조준 위치
}

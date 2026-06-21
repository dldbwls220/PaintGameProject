using UnityEngine;
using Fusion;

public struct NetworkInputData : INetworkInput
{
    public Vector3 _movementInput;
    public float _rotationInput;
    public NetworkBool _isJumpPressed;
    public NetworkBool _isShootPressed;
}

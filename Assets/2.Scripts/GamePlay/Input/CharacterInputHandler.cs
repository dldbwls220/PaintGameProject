using UnityEngine;

public class CharacterInputHandler : MonoBehaviour
{
    Vector3 _moveInputVector = Vector3.zero;
    bool _isJumpPressed;
    bool _isShootPressed;
    bool _isSquidPressed;
    Vector3 _aimTargetPosition;

    Camera _mainCam;
    MouseTarget _mouseTarget;

    void Start()
    {
        _mainCam = Camera.main;
    }

    public void SetMouseTarget(MouseTarget target)
    {
        _mouseTarget = target;
    }

    void Update()
    {
        _moveInputVector.x = Input.GetAxis("Horizontal");
        _moveInputVector.z = Input.GetAxis("Vertical");

        if (Input.GetKeyDown(KeyCode.Space)) _isJumpPressed = true;
        _isShootPressed = Input.GetMouseButton(0);
        _isSquidPressed = Input.GetKey(KeyCode.LeftShift);

        if (_mouseTarget != null)
            _aimTargetPosition = _mouseTarget.transform.position;
    }



    public NetworkInputData GetNetworkInput()
    {
        NetworkInputData inputdata = new NetworkInputData();

        inputdata._movementInput = _moveInputVector;
        inputdata._isJumpPressed = _isJumpPressed;
        inputdata._isShootPressed = _isShootPressed;
        inputdata._isSquidPressed = _isSquidPressed;

        // 카메라 방향을 struct에 담아 서버에서도 같은 방향으로 이동
        if (_mainCam != null)
        {
            Vector3 camForward = _mainCam.transform.forward;
            Vector3 camRight = _mainCam.transform.right;
            camForward.y = 0; camForward.Normalize();
            camRight.y = 0; camRight.Normalize();
            inputdata._cameraForwardRight = new Vector2(camForward.x, camForward.z);
        }

        inputdata._aimTargetPosition = _aimTargetPosition;
        _isJumpPressed = false; // 점프는 한 프레임만
        return inputdata;
    }
}

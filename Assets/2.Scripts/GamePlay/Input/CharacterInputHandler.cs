using UnityEngine;

public class CharacterInputHandler : MonoBehaviour
{
    Vector3 _moveInputVector = Vector3.zero;
    Vector3 _aimTargetPosition;
    float _climbAxis;
    float _sideAxis;
    bool _isJumpPressed;
    bool _isShootPressed;
    bool _isSquidPressed;
    public bool _isMenuOpen { get; set; }

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
        _climbAxis = Input.GetAxis("Vertical");
        _sideAxis = Input.GetAxis("Horizontal");


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
        inputdata._climbAxis = _climbAxis;
        inputdata._sideAxis = _sideAxis;
        inputdata._isMenuOpen = _isMenuOpen;

        // 카메라 방향을 struct에 담아 서버에서도 같은 방향으로 이동
        if (_mainCam != null)
        {
            Vector3 camForward = _mainCam.transform.forward;
            camForward.y = 0; camForward.Normalize();
            inputdata._cameraForward =new Vector2(camForward.x, camForward.z);
        }

        inputdata._aimTargetPosition = _aimTargetPosition;
        _isJumpPressed = false; 
        return inputdata;
    }
}

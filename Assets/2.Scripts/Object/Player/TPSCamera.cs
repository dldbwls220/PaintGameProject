using UnityEngine;
using Cinemachine;

public class TPSCamera : MonoBehaviour
{
    [SerializeField] NetworkInklingMovement _inkling;

    CinemachineFreeLook _freeLook;
    bool _lookInputEnabled = true;

    private void Start()
    {
        //initCam();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _freeLook = GetComponent<CinemachineFreeLook>();
        SetLookInputEnabled(false);
    }

    void Update()
    {
        // 인트로가 끝나기 전에는 마우스로 카메라가 돌아가면 안 됨
        bool introFinished = GameManager._instance != null && GameManager._instance._introFinished;

        if (introFinished != _lookInputEnabled)
        {
            SetLookInputEnabled(introFinished);
        }
    }

    void SetLookInputEnabled(bool enabled)
    {
        if (_freeLook == null) return;

        _freeLook.m_XAxis.m_InputAxisName = enabled ? "Mouse X" : string.Empty;
        _freeLook.m_YAxis.m_InputAxisName = enabled ? "Mouse Y" : string.Empty;

        if (!enabled)
        {
            // 축 이름을 비워도 마지막 입력값이 남아있으면 계속 회전하므로 함께 초기화
            _freeLook.m_XAxis.m_InputAxisValue = 0f;
            _freeLook.m_YAxis.m_InputAxisValue = 0f;
        }
        else
        {
            // Heading이 Target Forward 기준이라 X축 0 = 캐릭터가 바라보는 방향(정면) 뒤에 카메라 위치
            // Recentering이 꺼져 있어 자동으로 정렬되지 않으므로 인트로 종료 시점에 명시적으로 초기화

            if(_inkling._teamIndex == 1)
                _freeLook.m_XAxis.Value = 180f;
            else
                _freeLook.m_XAxis.Value = 0f;
           
            _freeLook.m_YAxis.Value = 0.5f;
        }

        _lookInputEnabled = enabled;
    }

    //public void initCam()
    //{
    //    _followCharacter = GameObject.FindGameObjectWithTag("Player").transform;

    //    _followCharacter.SendMessage("SetFollowCam", transform);
    //}
}

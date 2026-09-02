using Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class TPSCamera : MonoBehaviour
{
    [SerializeField] NetworkInklingMovement _inkling;
    [SerializeField]CinemachineVirtualCamera _killCam;

    [SerializeField] float _cameraMoveSpeed = 0.25f;

    const int _killCamOn = 20, _killCamOff = 0;
    CinemachineFreeLook _freeLook;
    CinemachineBrain _brain;

    bool _lookInputEnabled = true;

    private void Awake()
    {
        _brain = Camera.main.GetComponent<CinemachineBrain>();
    }

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
        bool introFinished = GameManager._instance != null && GameManager._instance._introFinished;

        if (introFinished != _lookInputEnabled)
        {
            SetLookInputEnabled(introFinished);
        }
    }

    public void EnableKillCam(Transform killer)
    {
        if (killer == null) return;
        _killCam.Follow = killer;
        _killCam.LookAt = killer;

        // 킬러 쪽으로 빠르게 날아감
        _brain.m_DefaultBlend =
            new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseOut, _cameraMoveSpeed);

        _killCam.Priority = _killCamOn;
        SetLookInputEnabled(false);
    }

    public void DisableKillCam()
    {
        // 리스폰 시 즉시 컷
        _brain.m_DefaultBlend =
            new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0f);

        _killCam.Priority = _killCamOff;
        _killCam.Follow = _killCam.LookAt = null;
    }

    void SetLookInputEnabled(bool enabled)
    {
        if (_freeLook == null) return;

        _freeLook.m_XAxis.m_InputAxisName = enabled ? "Mouse X" : string.Empty;
        _freeLook.m_YAxis.m_InputAxisName = enabled ? "Mouse Y" : string.Empty;

        if (!enabled)
        {
            _freeLook.m_XAxis.m_InputAxisValue = 0f;
            _freeLook.m_YAxis.m_InputAxisValue = 0f;
        }
        else
        {
            /// Heading이 Target Forward 기준이라 X축 0 = 캐릭터가 바라보는 방향(정면) 뒤에 카메라 위치
            /// Recentering이 꺼져 있어 자동으로 정렬되지 않으므로 인트로 종료 시점에 명시적으로 초기화

            if(_inkling._teamIndex == 1)
                _freeLook.m_XAxis.Value = 180f;
            else
                _freeLook.m_XAxis.Value = 0f;
           
            _freeLook.m_YAxis.Value = 0.5f;
        }

        _lookInputEnabled = enabled;
    }
}

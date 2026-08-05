using DefineEnum;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartUIManager : MonoBehaviour
{
    [SerializeField] GameObject[] _selectedWnds;

    private void Start()
    {
        foreach (var wnd in _selectedWnds)
        {
            wnd.SetActive(false);
        }
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseCustomization();
        }
    }

    #region[OpenNClose]
    public void OpenSelectedStartWnd()
    {
        _selectedWnds[(int)MenuSelectionState.StartGame].SetActive(true);
    }
    public void OpenSelectedCustomizeWnd()
    {
        _selectedWnds[(int)MenuSelectionState.Customization].SetActive(true);
    }
    public void OpenSelectedSettingWnd()
    {
        _selectedWnds[(int)MenuSelectionState.Setting].SetActive(true);
    }
    public void OpenSelectedExitWnd()
    {
        _selectedWnds[(int)MenuSelectionState.Exit].SetActive(true);
    }

    public void CloseSelectedStartWnd()
    {
        _selectedWnds[(int)MenuSelectionState.StartGame].SetActive(false);
    }
    public void CloseSelectedCustomizeWnd()
    {
        _selectedWnds[(int)MenuSelectionState.Customization].SetActive(false);
    }
    public void CloseSelectedSettingWnd()
    {
        _selectedWnds[(int)MenuSelectionState.Setting].SetActive(false);
    }
    public void CloseSelectedExitWnd()
    {
        _selectedWnds[(int)MenuSelectionState.Exit].SetActive(false);
    }
    #endregion[OpenNClose]


    public void MoveToLobby()
    {
        WipeTransitionManager.instance.LoadScene(SceneState.LobbyScene);
    }

    public void MoveToCustomization()
    {
        WipeTransitionManager.instance.OpenCustomizationScene();
    }

   public void PlayClickSound()
    {
        GameSoundManager.instance.PlayerSFX(PlayerSFXName.UI_Decide00);
    }

    public void CloseCustomization()
    {
        WipeTransitionManager.instance.CloseCustomizationScene();
    }

    public void EndGame()
    {
#if UNITY_EDITOR
        // 에디터에서 실행 중일 때 - 플레이 모드 종료
        EditorApplication.isPlaying = false;
#else
        // 빌드된 게임에서 실행 중일 때 - 애플리케이션 종료
        Application.Quit();
#endif
    }
}

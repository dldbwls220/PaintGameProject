using UnityEngine;
using UnityEngine.UI;

public class SettingUI : MonoBehaviour
{
    [Header("Mouse Sensitivity")]
    [SerializeField] Slider _mouseSlider;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && gameObject.activeSelf)
        {          
            CloseWnd();
        }
    }

    public void InitSliderValue()
    {
        if(PlayerCustomizeManager.instance == null) return;

        _mouseSlider.value = PlayerCustomizeManager.instance._mouseSensitivity;
    }

    public void SaveSensitivity()
    {
        if (PlayerCustomizeManager.instance == null) return;

        PlayerCustomizeManager.instance.SetSensitivity(_mouseSlider.value);
    }

    public void OpenWnd()
    {
        gameObject.SetActive(true);
        InitSliderValue();
    }

    public void CloseWnd()
    {
        SaveSensitivity();
        gameObject.SetActive(false);
    }
}

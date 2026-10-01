using DefineEnum;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingUI : MonoBehaviour
{
    [Header("Wnidonws")]
    [SerializeField] GameObject _mouseWnd;
    [SerializeField] GameObject _volumeWnd;
    [SerializeField] GameObject _resoultionWnd;
    [SerializeField] Color _selectedColor;

    [Header("Mouse Sensitivity")]
    [SerializeField] Slider _mouseSlider;

    [Header("Volume")]
    [SerializeField] AudioMixer _mixer;
    [SerializeField] Slider _masterSlider;
    [SerializeField] Slider _bgmSlider;
    [SerializeField] Slider _sfxSlider;

    [Header("Buttons")]
    [SerializeField] Image _mouseBtnImg;
    [SerializeField] Image _volumeBtnImg;
    [SerializeField] Image _resolutionBtnImg;

    [Header("Display")]
    [SerializeField] DisplaySettingUI _displayUI;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && gameObject.activeSelf)
        {          
            CloseWnd();
        }
    }

    public void InitSettingUI()
    {
        if(PlayerCustomizeManager.instance == null) return;

        _mouseSlider.value = PlayerCustomizeManager.instance.ReturnSensitivity();

        _masterSlider.value = PlayerCustomizeManager.instance.ReturnVolume(MixerState.MasterMixer);
        _bgmSlider.value = PlayerCustomizeManager.instance.ReturnVolume(MixerState.BGMMixer);
        _sfxSlider.value = PlayerCustomizeManager.instance.ReturnVolume(MixerState.SFXMixer);

        _displayUI.InitDisplay();
        _displayUI.InitDisplayOption();
    }

    public void SaveSensitivity()
    {
        if (PlayerCustomizeManager.instance == null) return;

        PlayerCustomizeManager.instance.SetSensitivity(_mouseSlider.value);
    }

    void SetVolume(MixerState state, float value)
    {
        float v = Mathf.Clamp(value, 0.0001f, 1f);
        _mixer.SetFloat(state.ToString(), Mathf.Log10(v) * 20f);   // 0~1 ¡æ -80~0 dB
        PlayerPrefs.SetFloat(state.ToString(), value);

        if (GameSoundManager.instance != null)
            GameSoundManager.instance.UpdateVolume(value, state);

    }

    public void SetMasterVolume(float value) => SetVolume(MixerState.MasterMixer, value);
    public void SetBGMVolume(float value) => SetVolume(MixerState.BGMMixer, value);
    public void SetSFXVolume(float value) => SetVolume(MixerState.SFXMixer, value);

    public void OpenWnd()
    {
        gameObject.SetActive(true);
        InitSettingUI();
        GameSoundManager.instance.PlayerSFX(PlayerSFXName.UI_Decide00);
    }

    public void CloseWnd()
    {
        SaveSensitivity();
        gameObject.SetActive(false);
        //GameSoundManager.instance.PlayerSFX(PlayerSFXName.UI_Decide00);
    }

    #region[EventSystem]

    public void OpenMouseSetting(bool isStart)
    {
        if (_mouseWnd.activeSelf) return;

        _mouseWnd.SetActive(true);
        _volumeWnd.SetActive(false);
        _resoultionWnd.SetActive(false);

        _mouseBtnImg.color = _selectedColor;
        _volumeBtnImg.color = Color.black;
        _resolutionBtnImg.color = Color.black;

        if (!isStart)
            GameSoundManager.instance.PlayerSFX(PlayerSFXName.UI_Decide00);
    }

    public void OpenVolumeSetting()
    {
        if (_volumeWnd.activeSelf) return;

        _mouseWnd.SetActive(false);
        _volumeWnd.SetActive(true);
        _resoultionWnd.SetActive(false);

        _mouseBtnImg.color = Color.black;
        _volumeBtnImg.color = _selectedColor;
        _resolutionBtnImg.color = Color.black;

        GameSoundManager.instance.PlayerSFX(PlayerSFXName.UI_Decide00);
    }

    public void OpenResoltuionSetting()
    {
        if (_resoultionWnd.activeSelf) return;

        _mouseWnd.SetActive(false);
        _volumeWnd.SetActive(false);
        _resoultionWnd.SetActive(true);

        _mouseBtnImg.color = Color.black;
        _volumeBtnImg.color = Color.black;
        _resolutionBtnImg.color = _selectedColor;

        GameSoundManager.instance.PlayerSFX(PlayerSFXName.UI_Decide00);
    }

    #endregion[EventSystem]
}

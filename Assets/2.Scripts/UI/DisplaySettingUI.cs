using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DisplaySettingUI : MonoBehaviour
{
    [Header("Dropbox")]
    [SerializeField]TMP_Dropdown _resolutionDropDown;

    [Header("Display Mode")]
    [SerializeField] Color _originColor;
    [SerializeField] Color _selectedColor;
    [SerializeField] Color _mouseOnColor;
    [SerializeField] Image _fsMode;
    [SerializeField] Image _wMode;


    List<Resolution> _resolutions = new List<Resolution>();
    Resolution _currResolution;
    int _selectResolution;
    bool _isFullScreen;

    public void InitDisplay()
    {
        _resolutions.Clear();

        Dictionary<(int width, int height), Resolution> bestByResolution = new Dictionary<(int, int), Resolution>();

        for (int i = 0; i < Screen.resolutions.Length; i++)
        {
            Resolution res = Screen.resolutions[i];

            if (res.width * 9 != res.height * 16 || res.width < 1280)
            {
                continue;
            }

            var key = (res.width, res.height);
            if (!bestByResolution.TryGetValue(key, out Resolution existing) || res.refreshRateRatio.value > existing.refreshRateRatio.value)
            {
                bestByResolution[key] = res;
            }
        }

        _resolutions.AddRange(bestByResolution.Values);
        _resolutions.Sort((a, b) => a.width != b.width ? a.width.CompareTo(b.width) : a.height.CompareTo(b.height));

        _resolutionDropDown.options.Clear();

        foreach (Resolution resolution in _resolutions)
        {
            TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData();

            option.text = resolution.width + " x " + resolution.height;
            _resolutionDropDown.options.Add(option);
        }
    }

    public void InitDisplayOption()
    {
        if (PlayerCustomizeManager.instance != null && PlayerCustomizeManager.instance.GetResolutionIdx() < 0)
        {
            GetCurrentResolution();
        }
        else
        {
            _selectResolution = PlayerCustomizeManager.instance.GetResolutionIdx();
        }

        _currResolution = _resolutions[_selectResolution];
        _resolutionDropDown.SetValueWithoutNotify(_selectResolution);
        _resolutionDropDown.RefreshShownValue();

        _isFullScreen = PlayerCustomizeManager.instance.GetFullScreen();
        SelectedButtonColor();
    }

    public void DropboxOptionChange(int x)
    {
        _selectResolution = x;
        _currResolution = _resolutions[_selectResolution];
        PlayerCustomizeManager.instance.SetResolutionIdx(x);
        Screen.SetResolution(_currResolution.width, _currResolution.height, _isFullScreen);
    }

    public void SetDisplayMode(bool isFullscreen)
    {
        if (isFullscreen && _isFullScreen) return;

        _isFullScreen = isFullscreen;

        PlayerCustomizeManager.instance.SetFullScreen(_isFullScreen);

        SelectedButtonColor();

        Screen.SetResolution(_currResolution.width, _currResolution.height, _isFullScreen);
    }

    public void FSMouseOnColor(bool isMouseOn)
    {
        if (isMouseOn && !_isFullScreen)
        {
            _fsMode.color = _mouseOnColor;
        }
        else if (!isMouseOn && !_isFullScreen)
        {
            _fsMode.color = _originColor;
        }
    }

    public void WMouseOnColor(bool isMouseOn)
    {
        if (isMouseOn && _isFullScreen)
        {
            _wMode.color = _mouseOnColor;
        }
        else if (!isMouseOn && _isFullScreen)
        {
            _wMode.color = _originColor;
        }
    }

    public void SelectedButtonColor()
    {
        if (_isFullScreen)
        {
            _fsMode.color = _selectedColor;
            _wMode.color = _originColor;
        }
        else
        {
            _fsMode.color = _originColor;
            _wMode.color = _selectedColor;
        }
    }

    void GetCurrentResolution()
    {
        int idx = 0;

        foreach (Resolution res in _resolutions)
        {           
            if (res.height == Screen.currentResolution.height && res.width == Screen.currentResolution.width)
            {
                _selectResolution = idx;
                PlayerCustomizeManager.instance.SetResolutionIdx(idx);
            }
            idx++;
        }
    }
}

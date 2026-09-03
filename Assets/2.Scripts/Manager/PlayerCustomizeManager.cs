using DefineEnum;
using DefineStructure;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerCustomizeManager : Singleton<PlayerCustomizeManager>
{
    //커스터마이징
    GameObject _emoteCharacterPrefab;
    PlayerCustomization _customization;
    PlayerData _data;
    public PlayerCustomization Customization => _customization;
    public PlayerData Data => _data;

    public void initDefaultcustom()
    {
        if (_emoteCharacterPrefab != null) return;

        _customization = PlayerCustomization.Default;
        LoadNInitCustomCharacter();

        SetSensitivity(2);
        UpdateVolume(1, MixerState.MasterMixer);
        UpdateVolume(1, MixerState.BGMMixer);
        UpdateVolume(1, MixerState.SFXMixer);
    }

    #region[커스터마이징]


    public void InstantiateCharacter(Transform pos)
    {
        Instantiate(_emoteCharacterPrefab, pos);
    }

    public void SaveCharacter(GameObject obj)
    {
        _emoteCharacterPrefab = obj;
    }

    void LoadNInitCustomCharacter()
    {
        GameObject go = Resources.Load<GameObject>("Object/Player/EmoteCharacter");

        _emoteCharacterPrefab = go;
    }

    public void SetHead(HeadState head)
    {
        PlayerCustomization c = _customization;
        c._head = head;
        _customization = c;
    }

    public void SetBody(BodyState body)
    {
        PlayerCustomization c = _customization;
        c._cloth = body;
        _customization = c;
    }

    public void SetShoe(ShoeState shoe)
    {
        PlayerCustomization c = _customization;
        c._shoes = shoe;
        _customization = c;
    }

    public void SetHair(HairState hair)
    {
        PlayerCustomization c = _customization;
        c._hair = hair;
        _customization = c;
    }

    public void SetEyebrowa(EyebrowsState eyebrowa)
    {
        PlayerCustomization c = _customization;
        c._eyebrows = eyebrowa;
        _customization = c;
    }

    public void SetNickname(string name)
    {
        PlayerData data = _data;
        data._nickName = name;
        _data = data;
    }

    #endregion[커스터마이징]

    #region[마우스 감도]
    
    public void SetSensitivity(float rate)
    {
        _data._mouseSensitivity = rate;
    }

    public float ReturnSensitivity()
    {
        return _data._mouseSensitivity;
    }

    #endregion[마우스 감도]

    #region[볼륨]

    public void UpdateVolume(float volume, MixerState state)
    {
        switch (state)
        {
            case MixerState.MasterMixer:
                _data._masterVolume = volume;
                break;
            case MixerState.BGMMixer:
                _data._bgmVolume = volume;
                break;
            case MixerState.SFXMixer:
                _data._sfxVolume = volume;
                break;
        }
    }

    public float ReturnVolume(MixerState state)
    {
        float volume = 0;

        switch (state)
        {
            case MixerState.MasterMixer:
                volume = _data._masterVolume;
                break;
            case MixerState.BGMMixer:
                volume = _data._bgmVolume;
                break;
            case MixerState.SFXMixer:
                volume = _data._sfxVolume;
                break;
        }

        return volume;
    }

    #endregion[볼륨]
}

using DefineEnum;
using DefineStructure;
using UnityEngine;

public class PlayerCustomizeManager : Singleton<PlayerCustomizeManager>
{
    //커스터마이징
    GameObject _emoteCharacterPrefab;
    PlayerCustomization _customization;
    PlayerData _data;
    public PlayerCustomization Customization => _customization;
    public PlayerData Data => _data;

    //마우스 감도
    public float _sensitivity;
    public float _mouseSensitivity => _sensitivity;

    public void initDefaultcustom()
    {
        if (_emoteCharacterPrefab != null) return;

        _customization = PlayerCustomization.Default;
        LoadNInitCustomCharacter();

        _sensitivity = 2;
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
        _sensitivity = rate;
    }

    #endregion[마우스 감도]
}

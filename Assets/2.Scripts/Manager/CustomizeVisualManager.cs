using UnityEngine;

public class CustomizeVisualManager : MonoBehaviour
{
    static CustomizeVisualManager _uniqueinstance;

    [Header("Custize Character Setting")]
    [SerializeField] GameObject _rootObj;
    CharacterClothChanger _clothChanger;
    SkinnedMeshRenderer[] _skins;
    public static CustomizeVisualManager _instance => _uniqueinstance;

    private void Awake()
    {
        _uniqueinstance = this;
    }

    public void UpdateClothing()
    {
        _clothChanger.SetCustomization(PlayerCustomizeManager.instance.Customization);
        _skins = _rootObj.transform.GetChild(0).transform.GetComponentsInChildren<SkinnedMeshRenderer>();
    }

    public void LoadCustomizeCharacter()
    {
        if (PlayerCustomizeManager.instance == null) return;

        PlayerCustomizeManager.instance.InstantiateCharacter(_rootObj.transform);

        _clothChanger = _rootObj.transform.GetComponentInChildren<CharacterClothChanger>();

        _clothChanger.SetCustomization(PlayerCustomizeManager.instance.Customization);

        _skins = _rootObj.transform.GetChild(0).transform.GetComponentsInChildren<SkinnedMeshRenderer>();
    }

    public void ExitCustomizeScene()
    {
        if (PlayerCustomizeManager.instance == null) return;

        PlayerCustomizeManager.instance.SaveCharacter(_rootObj.transform.GetChild(0).gameObject);
        CloseObject();
    }

    public void CloseObject()
    {
        this.gameObject.SetActive(false);
    }

    public void OpenObject()
    {
        this.gameObject.SetActive(true);
    }
}

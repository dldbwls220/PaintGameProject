using DefineEnum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapSelectUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Image _mapIcon;
    [SerializeField] Image[] _buttons;
    [SerializeField] TextMeshProUGUI _mapName;

    MapState _currentMap;
    int _mapIndex;

    public void InitMap(MapState state)
    {
        _currentMap = state;
        _mapIndex = (int)_currentMap;

    }

    public void ShowMap(MapState state)
    {
        _mapIcon.sprite = ResourcePoolManager.instance.Get<Sprite>(PoolDataType.MAPIMG, state.ToString());       
        KoreanName name = (KoreanName)((int)state);
        string koreanName = name.ToString().Replace("_", " ");
        _mapName.text = koreanName;
    }

    public void NextOption()
    {

        _mapIndex++;

        if (_mapIndex >= (int)MapState.Count)
        {
            _mapIndex = 0;
        }
        _currentMap = (MapState)_mapIndex;

        GameSoundManager.instance.PlayerSFX(PlayerSFXName.CustomizeUI_Decide);
    }

    public void BackOption()
    {
        _mapIndex--;

        if (_mapIndex < 0)
        {
            _mapIndex = (int)MapState.Count - 1;
        }
        _currentMap = (MapState)_mapIndex;

        GameSoundManager.instance.PlayerSFX(PlayerSFXName.CustomizeUI_Decide);
    }

    public void CloseButtons()
    {
        foreach (Image button in _buttons)
        {
            button.enabled = false;
        }
    }

}

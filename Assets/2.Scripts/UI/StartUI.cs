using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _mapName;

    public void InitUI(string name)
    {
        MapName(name);
    }

    public void OpenWnd()
    {
        gameObject.SetActive(true);
    }

    public void MapName(string name)
    {
        _mapName.text = name;
    }

    public void CloseWnd()
    {
        gameObject.SetActive(false);
    }
}

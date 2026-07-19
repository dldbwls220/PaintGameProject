using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BeatenInfo : MonoBehaviour
{
    [Header("Beaten Info Setting")]
    [SerializeField] TextMeshProUGUI _weaponText;
    [SerializeField] Image _weaponIcon;
    [SerializeField] Image[] _inkColor;

    //나중에 플레이어 정보가 담긴 struct를 이용하기

    public void OpenWnd()
    {
        gameObject.SetActive(true);
    }

    public void BeatenInfotxt(string weaponText)
    {
        weaponText = weaponText + "에\n당했다!";
    }

    public void SetColor(Color color)
    {
        foreach (var ink in _inkColor)
        {
            ink.color = color;
        }
    }

    public void CloseWnd()
    {
        gameObject?.SetActive(false);
    }
}

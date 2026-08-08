using TMPro;
using UnityEngine;

public class KillLogUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _text;

    public void KillLogText(string victimName)
    {
        _text.text = victimName + "를 쓰러뜨렸다!";
    }
}

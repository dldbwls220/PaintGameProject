using TMPro;
using UnityEngine;

public class TimerUI : MonoBehaviour
{
    [SerializeField]TextMeshProUGUI _time;

    public void OpenWnd()
    {
        gameObject.SetActive(true);
    }

    public void CloseWnd()
    {
        gameObject.SetActive(false);
    }

    public void SetTime(float time)
    {
        int minutes = (int)(time / 60f);
        int seconds = (int)(time % 60f);

        if (time <= 60)
            _time.color = Color.yellow;
        else
            _time.color = Color.white;

        _time.text = $"{minutes:0}:{seconds:00}";
    }
}

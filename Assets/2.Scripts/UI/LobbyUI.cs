using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DefineEnum;

public class LobbyUI : MonoBehaviour
{
    [Header("Connected Players")]
    [SerializeField] LobbyPlayerUI[] _players;

    [Header("Start Button")]
    [SerializeField] GameStartBtnUI _btn;

    [Header("Start Timer")]
    [SerializeField] TextMeshProUGUI _timer;


    public int SlotCount => _players.Length;

    private void Start()
    {
        foreach (var player in _players)
        {
            player.OpenPlayerBar();
        }
    }

    public void JoinUser(int idx , string name, bool isMe)
    {
        _players[idx].InitPlayerBar(name, isMe);
    }

    public void LeaveUser(int idx)
    {
        _players[idx].PlayerExit();
    }

    public bool PressStart(bool nowPress)
    {
        return _btn.PressFillSlide(nowPress);
    }

    public void CloseStartBtn()
    {
        _btn.CloseWnd();
    }

    public void SetTimer(float time)
    {
        //int seconds = Mathf.CeilToInt(time);
        //_timer.text = $"{seconds / 60:00}:{seconds % 60:00}";

        int sec = (int)time;

        if (time <= 1)
        {
            _timer.text = "START";
        }
        else
            _timer.text = sec.ToString();
    }
}

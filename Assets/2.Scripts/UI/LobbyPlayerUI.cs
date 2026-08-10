using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerUI : MonoBehaviour
{
    [SerializeField] Image _arrow;
    [SerializeField] TextMeshProUGUI _playerNameTxt;
    [SerializeField] Animation _barAnim;
    AnimationState _state;

    public void OpenPlayerBar()
    {
        _arrow.enabled = false;
        _state = _barAnim[_barAnim.clip.name];
    }

    public void InitPlayerBar(string playerName, bool isMe = false)
    {
        if(isMe)
            _arrow.enabled = true;

        _playerNameTxt.text = playerName;
        PlayAnimForward();
    }

    public void SetName(string playerName)
    {
        _playerNameTxt.text = playerName;
    }

    public void PlayerExit()
    {
        _arrow.enabled = false;
        PlayAnimReverse();
    }

    void PlayAnimForward()
    {
        _state.speed = 1;
        _state.time = 0;
        _barAnim.Play();
    }

    void PlayAnimReverse()
    {
        _state.speed = -1;
        _state.time = _state.length;
        _barAnim.Play();
    }
}

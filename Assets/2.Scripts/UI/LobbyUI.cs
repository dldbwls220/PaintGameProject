using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] LobbyPlayerUI[] _players;

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
}

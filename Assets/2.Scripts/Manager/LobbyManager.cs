using Fusion;
using UnityEngine;
using DefineEnum;
using System.Collections.Generic;

public class LobbyManager : NetworkBehaviour
{
    static LobbyManager _uniqueinstance;

    [SerializeField] SceneState _gameSceneState = SceneState.LobbyScene;
    [SerializeField] LobbyUI _UI;
    [SerializeField] float _startDelay = 60f;
    [SerializeField] float _fullLobbyStartDelay = 30f;

    [Networked, Capacity(8)]
    public NetworkDictionary<PlayerRef, int> PlayerSlots => default;

    [Networked] TickTimer StartTimer { get; set; }
    [Networked] NetworkBool _isFull { get; set; }

    readonly Dictionary<PlayerRef, int> _shownSlots = new();

    public static LobbyManager _instance => _uniqueinstance;

    private void Awake()
    {
        _uniqueinstance = this;
    }

    public override void Render()
    {
        List<PlayerRef> left = null;
        foreach (var kv in _shownSlots)
        {
            if (!PlayerSlots.ContainsKey(kv.Key))
            {
                _UI.LeaveUser(kv.Value);
                (left ??= new List<PlayerRef>()).Add(kv.Key);
            }
        }
        if (left != null)
        {
            foreach (var player in left) _shownSlots.Remove(player);
        }

        foreach (var kv in PlayerSlots)
        {
            if (!_shownSlots.ContainsKey(kv.Key))
            {
                _UI.JoinUser(kv.Value, "testname", kv.Key == Runner.LocalPlayer);
                _shownSlots[kv.Key] = kv.Value;
            }
        }

        if (!HasStateAuthority)
        {
            _UI.CloseStartBtn();
        }

        _UI.SetTimer(StartTimer.RemainingTime(Runner) ?? 0f);
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority)
        {
            if (Input.GetKey(KeyCode.Space))
            {
                if (_UI.PressStart(true))
                {
                    ForceStart();
                }
            }
            else
                _UI.PressStart(false);

            if (StartTimer.Expired(Runner))
            {
                StartTimer = TickTimer.None;
                ForceStart();
            }
        }
    }

    public void ForceStart()
    {
        RPC_StartWipeTransition(_gameSceneState);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_StartWipeTransition(SceneState state)
    {
        WipeTransitionManager.instance.LoadScene(state);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!HasStateAuthority) return;

        int idx = GetNextFreeIndex();
        if (idx < 0)
        {
            Debug.LogWarning("OnPlayerJoined: 로비 슬롯이 가득 찼습니다.");
            return;
        }

        PlayerSlots.Set(player, idx);

        if (_UI.SlotCount == PlayerSlots.Count)
        {
            _isFull = true;
            StartTimer = TickTimer.CreateFromSeconds(Runner, _fullLobbyStartDelay);
        }
        else
            StartTimer = TickTimer.CreateFromSeconds(Runner, _startDelay);
    }

    public void OnPlayerLeft(PlayerRef player)
    {
        if (!HasStateAuthority) return;
        if (!PlayerSlots.ContainsKey(player)) return;

        PlayerSlots.Remove(player);

        if (_isFull)
        {
            StartTimer = TickTimer.CreateFromSeconds(Runner, _startDelay);
            _isFull = false;
        }
    }

    int GetNextFreeIndex()
    {
        int slotCount = _UI.SlotCount;
        for (int i = 0; i < slotCount; i++)
        {
            bool used = false;
            foreach (var kv in PlayerSlots)
            {
                if (kv.Value == i)
                {
                    used = true;
                    break;
                }
            }

            if (!used) return i;
        }

        return -1;
    }
}

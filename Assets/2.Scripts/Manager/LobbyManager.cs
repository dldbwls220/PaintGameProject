using Fusion;
using UnityEngine;
using DefineEnum;
using System.Collections.Generic;

public class LobbyManager : NetworkBehaviour
{
    static LobbyManager _uniqueinstance;

    public MapState _selectedMap = MapState.Port_Mackerel;
    [SerializeField] SceneState _gameSceneState = SceneState.LobbyScene;
    [SerializeField] LobbyUI _UI;
    [SerializeField] float _startDelay = 60f;
    [SerializeField] float _fullLobbyStartDelay = 30f;
    [SerializeField] MapSelectUI _mapSelectUI;

    [Networked, Capacity(8)]
    public NetworkDictionary<PlayerRef, int> PlayerSlots => default;

    [Networked, Capacity(8)]
    public NetworkDictionary<PlayerRef, NetworkString<_32>> PlayerNicknames => default;

    [Networked] TickTimer StartTimer { get; set; }
    [Networked] NetworkBool _isFull { get; set; }

    [Networked] NetworkBool _isWaiting { get; set; }
    [Networked] public MapState _currentMap { get; set; } = MapState.Port_Mackerel;
    MapState _shownMap = (MapState)(-1);

    bool _wasWaiting;

    readonly Dictionary<PlayerRef, int> _shownSlots = new();
    readonly Dictionary<PlayerRef, string> _shownNicknames = new();

    public static LobbyManager _instance => _uniqueinstance;

    private void Awake()
    {
        _uniqueinstance = this;
    }

    public override void Spawned()
    {
        _isWaiting = true;

        var nickname = PlayerCustomizeManager.instance.Data._nickName;
        if (HasStateAuthority)
        {
            _currentMap = _selectedMap;
            PlayerNicknames.Set(Runner.LocalPlayer, nickname);
        }
        else
            RPC_SubmitNickname(nickname);
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
            foreach (var player in left)
            {
                _shownSlots.Remove(player);
                _shownNicknames.Remove(player);
            }
        }

        foreach (var kv in PlayerSlots)
        {
            if (!_shownSlots.ContainsKey(kv.Key))
            {
                string name = GetNickname(kv.Key);
                _UI.JoinUser(kv.Value, name, kv.Key == Runner.LocalPlayer);
                GameSoundManager.instance.PlayerSFX(PlayerSFXName.PlayerJoined, volume: 0.6f);

                _shownSlots[kv.Key] = kv.Value;
                _shownNicknames[kv.Key] = name;
            }
        }

        // 닉네임 RPC가 슬롯 입장보다 늦게 도착하거나 이후에 바뀔 수 있으므로 매 프레임 최신값과 비교해 갱신한다
        foreach (var kv in _shownSlots)
        {
            string name = GetNickname(kv.Key);
            if (_shownNicknames[kv.Key] != name)
            {
                _UI.UpdateUserName(kv.Value, name);
                _shownNicknames[kv.Key] = name;
            }
        }

        if (!HasStateAuthority)
        {
            _UI.CloseStartBtn();
            _mapSelectUI.CloseButtons();
        }

        _UI.SetTimer(StartTimer.RemainingTime(Runner) ?? 0f);

        if (!_isWaiting && _wasWaiting)
        {
            GameSoundManager.instance.PlayerSFX(PlayerSFXName.BattleStartBell, volume: 0.2f);
        }

        _wasWaiting = _isWaiting;

        _mapSelectUI.ShowMap(_currentMap);

        if (_shownMap != _currentMap)
        {
            if (_shownMap != (MapState)(-1))
                GameSoundManager.instance.PlayerSFX(PlayerSFXName.CustomizeUI_Decide); // 모두가 클릭음 들음
            _shownMap = _currentMap;
        }
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

        if (StartTimer.RemainingTime(Runner) <= 1)
        {
            _isWaiting = false;
        }
    }

    public void ForceStart()
    {
        RPC_StartWipeTransition(_gameSceneState, _currentMap);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_StartWipeTransition(SceneState state, MapState mapState)
    {
        WipeTransitionManager.instance.LoadScene(state, mapState);
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

    string GetNickname(PlayerRef player)
    {
        if (!PlayerNicknames.TryGet(player, out var nickname) || nickname.Length == 0)
            return "잉클링";

        return nickname.Value;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_SubmitNickname(NetworkString<_32> nickname, RpcInfo info = default)
    {
        PlayerNicknames.Set(info.Source, nickname);
    }

    public void OnClickNextMap()
    {
        if(HasStateAuthority)
            _currentMap = Step(_currentMap, +1);
        else
            RPC_RequestChangeMap(+1);
    }

    public void OnClickBackMap()
    {
        if (HasStateAuthority)
            _currentMap = Step(_currentMap, -1);
        else
            RPC_RequestChangeMap(-1);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_RequestChangeMap(int dir)
    {
        _currentMap = Step(_currentMap, dir);
    }

    static MapState Step(MapState m, int dir)
    {
        int count = (int)MapState.Count;
        int i = ((int)m + dir % count + count) % count;
        return (MapState)i;
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

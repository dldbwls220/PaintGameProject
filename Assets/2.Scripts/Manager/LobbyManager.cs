using Fusion;
using UnityEngine;
using DefineEnum;
using System.Collections.Generic;

public class LobbyManager : NetworkBehaviour
{
    static LobbyManager _uniqueinstance;

    // TODO: 임시 테스트용. 실제 강제시작 버튼 UI가 생기면 이 Update의 스페이스바 체크는 제거하고
    // 버튼 OnClick에서 ForceStart()를 직접 호출하도록 바꿀 것.
    [SerializeField] SceneState _gameSceneState = SceneState.LobbyScene;
    [SerializeField] LobbyUI _lobbyUI;

    NetworkRunner _runner;

    [Networked, Capacity(8)]
    public NetworkDictionary<PlayerRef, int> PlayerSlots => default;

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
                _lobbyUI.LeaveUser(kv.Value);
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
                _lobbyUI.JoinUser(kv.Value, "testname", kv.Key == Runner.LocalPlayer);
                _shownSlots[kv.Key] = kv.Value;
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ForceStart();
        }
    }

    public void ForceStart()
    {
        if (_runner == null)
        {
            _runner = FindFirstObjectByType<NetworkRunner>();
        }

        if (_runner == null || !_runner.IsRunning)
        {
            Debug.LogWarning("ForceStart: NetworkRunner를 찾지 못했거나 아직 실행 중이 아닙니다.");
            return;
        }

        if (!_runner.IsServer)
        {
            Debug.Log("ForceStart: 호스트만 게임을 시작할 수 있습니다.");
            return;
        }

        _runner.SessionInfo.IsOpen = false;
        _runner.SessionInfo.IsVisible = false;
        _runner.LoadScene(SceneRef.FromIndex((int)_gameSceneState));
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        int idx = GetNextFreeIndex();
        if (idx < 0)
        {
            Debug.LogWarning("OnPlayerJoined: 로비 슬롯이 가득 찼습니다.");
            return;
        }

        PlayerSlots.Set(player, idx);
    }

    public void OnPlayerLeft(PlayerRef player)
    {
        if (!PlayerSlots.ContainsKey(player)) return;

        PlayerSlots.Remove(player);
    }

    int GetNextFreeIndex()
    {
        int slotCount = _lobbyUI.SlotCount;
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

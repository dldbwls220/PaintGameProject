using DefineStructure;
using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    static GameManager _uniqueinstance;

    [SerializeField] GameObject _gameUIManager;
    [SerializeField] NetworkInklingMovement _playerPrefab;
    [SerializeField] float _gameDuration = 180f;
    [SerializeField] float _respawnTime = 9f;

    [SerializeField] Color[] _teamColors1;
    [SerializeField] Color[] _teamColors2;

    int _spawnCount;
    bool _inkIdxAssigned;
    GameUIManager _uiManager;

    [Networked]
    [Capacity(32)]
    [HideInInspector]
    public NetworkDictionary<PlayerRef, PlayerData> PlayerData { get; }

    [Networked]
    [HideInInspector]
    public TickTimer RemainingTime { get; set; }

    [Networked] public int _inkIdx { get; private set; }

    public static GameManager _instance => _uniqueinstance;

    void Awake()
    {
        _uniqueinstance = this;
    }

    void Start()
    {
        GameObject ui = Instantiate(_gameUIManager);
        _uiManager = ui.GetComponentInChildren<GameUIManager>();
        _uiManager.InitUI();
    }

    // 서버(Spawner.OnPlayerJoined)에서만 호출됨: 해당 플레이어의 PlayerData를 먼저 등록한 뒤 캐릭터를 스폰한다
    // (캐릭터의 Spawned()가 PlayerData를 즉시 읽어가므로, 등록이 스폰보다 먼저 끝나야 한다)
    public void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        _spawnCount++;
        int spawnIndex = _spawnCount;
        int teamIndex = spawnIndex % 2;

        EnsureInkVariantAssigned(spawnIndex);

        var playerData = new PlayerData
        {
            _playerRef = player,
            _teamColor = GetTeamColor(teamIndex),
            _enemyColor = GetEnemyColor(teamIndex),
            _statisticPostion = int.MaxValue,
            _isAlive = true,
            _isConnected = true,
        };

        this.PlayerData.Set(player, playerData);

        runner.Spawn(_playerPrefab, Utils.GetSpawnPoint(), Quaternion.identity, player,
            onBeforeSpawned: (_, obj) =>
            {
                obj.GetComponent<NetworkPlayer>().SetSpawnIndex(spawnIndex);
            });
    }

    // 매치당 한 번만 뽑히는 잉크 색상 변형(팀 컬러 세트) 인덱스 — 새 매치 시작(spawnIndex == 1) 시 다시 뽑는다
    void EnsureInkVariantAssigned(int spawnIndex)
    {
        if (spawnIndex == 1 || !_inkIdxAssigned)
        {
            _inkIdx = Random.Range(0, _teamColors1.Length);
            _inkIdxAssigned = true;
        }
    }

    Color GetTeamColor(int teamIndex) => teamIndex == 1 ? _teamColors1[_inkIdx] : _teamColors2[_inkIdx];
    Color GetEnemyColor(int teamIndex) => teamIndex == 1 ? _teamColors2[_inkIdx] : _teamColors1[_inkIdx];
}

using DefineStructure;
using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    static GameManager _uniqueinstance;
    [Header("Class Reference")]
    [SerializeField] NetworkInklingMovement _playerPrefab;
    [Header("Gameplay Setting")]
    [SerializeField] float _gameDuration = 180f;
    [SerializeField] float _respawnTime = 9f;
    [Header("Team Colors")]
    [SerializeField] Color[] _teamColors1;
    [SerializeField] Color[] _teamColors2;
    [Header("Resources")]
    [SerializeField] GameObject _gameUIManager;
    [SerializeField] SpawnPlatform[] _spawnPlatforms;
    [SerializeField] GameObject _spawnRoot1;
    [SerializeField] GameObject _spawnRoot2;

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

    [Networked, HideInInspector] public int _inkIdx { get; private set; }

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

    public override void Render()
    {
        foreach (var platform in _spawnPlatforms)
        {
            platform.InitPlatform(_teamColors1[_inkIdx], _teamColors2[_inkIdx]);
        }
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
            _myRespawnTime = _respawnTime,
            _isAlive = true,
            _isConnected = true,
        };

        this.PlayerData.Set(player, playerData);
        //Vector3 spawnPos = GetSpawnPoint(teamIndex, spawnIndex).position;

        Vector3 spawnPos = Utils.GetSpawnPoint();

        Quaternion spawnRot = teamIndex == 1 ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;

        runner.Spawn(_playerPrefab, spawnPos, spawnRot, player,
            onBeforeSpawned: (_, obj) =>
            {
                obj.GetComponent<NetworkPlayer>().SetSpawnIndex(spawnIndex);
            });
    }

    Transform GetSpawnPoint(int teamIdx, int spawnIdx , bool isRespawn = false)
    {
        Transform spawnPoint = default;

        if (teamIdx == 1)
        {
            spawnPoint = _spawnRoot1.transform.GetChild(isRespawn == false ? (spawnIdx - 1) / 2 : 4);
        }
        else
        {
            spawnPoint = _spawnRoot2.transform.GetChild(isRespawn == false ? (spawnIdx - 1) / 2 : 4);
        }

        return spawnPoint;
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

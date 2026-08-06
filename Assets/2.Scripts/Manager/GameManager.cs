using DefineStructure;
using Fusion;
using DefineEnum;
using UnityEngine;
using UnityEngine.Playables;

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
    [Header("Timeline")]
    [SerializeField] PlayableDirector _playableDirector;
    [Header("GameSoundSetting")]
    [SerializeField] float _introFadeSpeed = 0.3f;
    [SerializeField] float _gameBGMFadeSpeed = 0.5f;
    OpeningBGMName _openingName;

    int _spawnCount;
    bool _inkIdxAssigned;
    GameUIManager _uiManager;
    bool _introPlaying;
    bool _readyUiShown;
    bool _isIntroBGMEnd;
    bool _introFadeOutComplete;
    bool _wasGameStart;

    [Networked]
    [Capacity(8)]
    [HideInInspector]
    public NetworkDictionary<PlayerRef, PlayerData> PlayerData { get; }

    [Networked]
    [HideInInspector]
    public TickTimer GameTime { get; set; }

    [Networked]
    [HideInInspector]
    public TickTimer ReadyTimer { get; set; }

    [Networked, HideInInspector] public int _inkIdx { get; private set; }
    [Networked] public NetworkBool _introFinished { get; set; }
    [Networked] public NetworkBool _gameStart { get; set; }
    [Networked] NetworkBool _introStarted { get; set; }
    [Networked] float _introStartTime { get; set; }

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

        _playableDirector.stopped += OnIntroFinished;
    }

    private void OnDestroy()
    {
        _playableDirector.stopped -= OnIntroFinished;
    }

    public override void Spawned()
    {
        // 인트로 시작 시점을 네트워크 동기 시간으로 한 번만 기록 (StateAuthority만 기록, 나머지는 복제받음)
        if (HasStateAuthority && !_introStarted)
        {
            _introStartTime = Runner.SimulationTime;
            _introStarted = true;
        }

        SetGameBGM();
      
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (_introFinished && !ReadyTimer.IsRunning)
        {
            ReadyTimer = TickTimer.CreateFromSeconds(Runner, 1.5f);
        }

        if (ReadyTimer.Expired(Runner) && !_gameStart)
        {
            _gameStart = true;
            GameTime = TickTimer.CreateFromSeconds(Runner, _gameDuration);
        }
    }

    public override void Render()
    {
        foreach (var platform in _spawnPlatforms)
        {
            platform.InitPlatform(_teamColors1[_inkIdx], _teamColors2[_inkIdx]);
        }

        StartIntroIfNeeded();
        FadeOutBGM();

        try
        {
            _uiManager.AssignPlayerStatus(PlayerData, Runner.LocalPlayer);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }

        if (_introFinished && !_readyUiShown)
        {
            _readyUiShown = true;
            _uiManager.StartReadyUI();
        }

        if (_gameStart)
        {          
            _uiManager.SetTime(GameTime.RemainingTime(Runner) ?? 0);
            if (!_wasGameStart)
            {
                GameSoundManager.instance.PlayerSFX(PlayerSFXName.Count01);
                GameSoundManager.instance._GameBGMDESC._unpause();
                _wasGameStart = true;
            }       
        }

        if (GameTime.RemainingTime(Runner) <= 61)
        {
            GameSoundManager.instance._GameBGMDESC._volum = Mathf.MoveTowards(GameSoundManager.instance._GameBGMDESC._volum, 0, _gameBGMFadeSpeed * Time.deltaTime);
            GameSoundManager.instance._NowOrNeverDESC._unpause();
        }
    }

    // 호스트/클라이언트가 각자 로컬 시점(Awake)에 재생을 시작하는 대신,
    // 네트워크 동기 시간(Runner.SimulationTime) 기준 경과 위치로 딱 한 번만 seek한 뒤 Play()한다.
    // 이후 재생은 Unity Timeline의 정상 자동 재생 흐름을 타므로 Signal Emitter 등이 원래대로 발동한다.
    void StartIntroIfNeeded()
    {
        if (_introPlaying || !_introStarted) return;

        GameSoundManager.instance.OpeningBGM(_openingName);

        double elapsed = Runner.SimulationTime - _introStartTime;
        if (elapsed < 0) elapsed = 0;

        _introPlaying = true;

        if (elapsed >= _playableDirector.duration)
        {
            // 인트로가 이미 끝난 시점에 뒤늦게 합류한 경우 재생 없이 바로 종료 처리
            OnIntroFinished(_playableDirector);
            return;
        }

        _playableDirector.time = elapsed;
        _playableDirector.Play();
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
            _teamColor = GetTeamColor(teamIndex),
            _enemyColor = GetEnemyColor(teamIndex),
            _statisticPostion = int.MaxValue,
            _myRespawnTime = _respawnTime,
            _isAlive = true,
            _isConnected = true,
            _teamIndex = teamIndex,
        };

        this.PlayerData.Set(player, playerData);
        Vector3 spawnPos = GetSpawnPoint(teamIndex, spawnIndex).position;

        Quaternion spawnRot = teamIndex == 1 ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;

        runner.Spawn(_playerPrefab, spawnPos, spawnRot, player,
            onBeforeSpawned: (_, obj) =>
            {
                obj.GetComponent<NetworkPlayer>().SetSpawnIndex(spawnIndex);
            });
    }

    // 각 클라이언트가 RPC로 보낸 커스터마이징 값을 해당 플레이어의 PlayerData에 반영한다 (StateAuthority에서만 호출됨)
    public void SetPlayerCustomization(PlayerRef player, PlayerCustomization custom)
    {
        if (!PlayerData.TryGet(player, out var data)) return;

        data._custom = custom;
        PlayerData.Set(player, data);
    }

    public Transform GetSpawnPoint(int teamIdx, int spawnIdx , bool isRespawn = false)
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

    void OnIntroFinished(PlayableDirector director)
    {
        _uiManager.OpenAllGamePlayUI();
        if (HasStateAuthority)
            _introFinished = true;
    }

    void FadeOutBGM()
    {
        if (_isIntroBGMEnd && !_introFadeOutComplete)
        {
            GameSoundManager.instance._UiBGMDESC._volum = Mathf.MoveTowards(GameSoundManager.instance._UiBGMDESC._volum, 0, _introFadeSpeed * Time.deltaTime);

            if (GameSoundManager.instance._UiBGMDESC._volum <= 0)
                _introFadeOutComplete = true;
        }
    }

    void SetGameBGM()
    {
        MusicType type = Random.value < 0.5f
            ? MusicType.Normal
            : (MusicType)(1 + Random.Range(0, (int)MusicType.Count - 1));
        int fesIdx = Random.Range(0, 2);

        switch (type)
        {
            case MusicType.Normal:
                _openingName = OpeningBGMName.Opening;

                NormalBGMName normal = (NormalBGMName)(Random.Range(0, (int)NormalBGMName.Count));

                GameSoundManager.instance.GameBGMNormal(normal);
                GameSoundManager.instance.NowOrNeverBGM(NowOrNever.NoworNever_Normal);
                break;
            case MusicType.SquidSisters:
                _openingName = OpeningBGMName.Fes_Battle_Opening;

                GameSoundManager.instance.GameBGMSquidSisters((SquidSisters)fesIdx);
                GameSoundManager.instance.NowOrNeverBGM(NowOrNever.NoworNever_SquidSisters);
                break;
            case MusicType.Tentacles:
                _openingName = OpeningBGMName.Fes_Battle_Opening;

                GameSoundManager.instance.GameBGMSquidTentacles((Tentacles)fesIdx);
                GameSoundManager.instance.NowOrNeverBGM(NowOrNever.NoworNever_Tentacles);
                break;
            case MusicType.DeepCut:
                _openingName = OpeningBGMName.Fes_Battle_Opening;

                GameSoundManager.instance.GameBGMSquidDeepCut((DeepCut)fesIdx);
                GameSoundManager.instance.NowOrNeverBGM(NowOrNever.NoworNever_DeepCut);
                break;
        }

        GameSoundManager.instance._GameBGMDESC._pause();
        GameSoundManager.instance._NowOrNeverDESC._pause();
    }

    Color GetTeamColor(int teamIndex) => teamIndex == 1 ? _teamColors1[_inkIdx] : _teamColors2[_inkIdx];
    Color GetEnemyColor(int teamIndex) => teamIndex == 1 ? _teamColors2[_inkIdx] : _teamColors1[_inkIdx];

    public void CloseStartUI()
    {
        _uiManager.CloseStartUI();
    }

    public void IntroBGMEnd()
    {
        _isIntroBGMEnd = true;
    }

}

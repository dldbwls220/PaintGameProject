using System.Collections;
using System.Collections.Generic;
using DefineStructure;
using Fusion;
using DefineEnum;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    static GameManager _uniqueinstance;

    [SerializeField] MapRootEntry[] _mapRoots;

    [Header("Class Reference")]
    [SerializeField] NetworkInklingMovement _playerPrefab;
    [SerializeField] GridManager _gridManager;

    [Header("Gameplay Setting")]
    [SerializeField] float _gameDuration = 180f;
    [SerializeField] float _respawnTime = 9f;

    [Header("Team Colors")]
    [SerializeField] Color[] _teamColors1;
    [SerializeField] Color[] _teamColors2;

    [Header("Resources")]
    [SerializeField] GameObject _gameUIManager;
    [SerializeField] SpawnPlatform[] _spawnPlatforms;
    [SerializeField] GameObject _spawnPlatformTeam1;
    [SerializeField] GameObject _spawnPlatformTeam2;
    [SerializeField] GameObject _spawnRoot1;
    [SerializeField] GameObject _spawnRoot2;

    [Header("Timeline")]
    [SerializeField] PlayableDirector _playableDirector;
    [SerializeField] PlayableAsset _introTeam1;
    [SerializeField] PlayableAsset _introTeam2;

    [Header("GameSoundSetting")]
    [SerializeField] float _introFadeSpeed = 0.3f;
    [SerializeField] float _gameBGMFadeSpeed = 0.5f;

    [Header("Camera")]
    [SerializeField] Camera _mainCamera;
    [SerializeField] GameObject _team1Dolly;
    [SerializeField] GameObject _team2Dolly;
    [SerializeField] GameObject _team1Fix;
    [SerializeField] GameObject _team2Fix;

    [Header("UI Setting")]
    const float _dangerRateGap = 0.30f;


    OpeningBGMName _openingName;

    int _spawnCount;
    bool _inkIdxAssigned;
    GameUIManager _uiManager;
    bool _introPlaying;
    bool _readyUiShown;
    bool _isIntroBGMEnd;
    bool _introFadeOutComplete;
    bool _wasGameStart;
    bool _introDirectorResolved;

    public bool _nowTeamCam { get; set; }

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
    [Networked] public NetworkBool _gameEnd { get; set; }
    [Networked] public NetworkBool _isJudging { get; set; }
    [Networked] public NetworkBool _isResultStarted { get; set; }
    [Networked] NetworkBool _introStarted { get; set; }
    [Networked] float _introStartTime { get; set; }
    [Networked] float _team1InkRate { get; set; }
    [Networked] float _team2InkRate { get; set; }
    [Networked] TickTimer _rateCalcTimer { get; set; }

    bool _wasResultStarted;
    bool _resultSequenceStarted;
  
    bool _wasMyTeamDanger;
    bool _wasEnemyTeamDanger;

    bool _wasTeam1Dead;
    bool _wasTeam2Dead;

    public MusicType musicType { get; private set; }
    public static GameManager _instance => _uniqueinstance;

    void Awake()
    {
        _uniqueinstance = this;
    }

    void Start()
    {
        MapState map = WipeTransitionManager.instance._mapState;
        foreach (var e in _mapRoots)
        {
            if (e._map == map)
            {
                e._root.SetActive(true);
                SpawnSPObj(e._root);
            }

        }

        // 활성화된 맵에 맞춰 해당 맵의 Ink Zone 텍스처 활성화
        //if (WorldInkZoneManager._instance != null)
        //    WorldInkZoneManager._instance.SwitchMap(map);

        if(MapManager._instance != null)
            MapManager._instance.SwitchMap(map);

        SetCameraPos();

        GameObject ui = Instantiate(_gameUIManager);
        _uiManager = ui.GetComponentInChildren<GameUIManager>();
        _uiManager.InitUI();


        CreatGrid();
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

        CalculateColorRate();

        //if(GameTime.RemainingTime(Runner) <= 1) { _gameEnd = true; }
    }

    public override void Render()
    {
        foreach (var platform in _spawnPlatforms)
        {
            platform.InitPlatform(_teamColors1[_inkIdx], _teamColors2[_inkIdx]);
        }

        StartIntroIfNeeded();
        FadeOutBGM();
        CheckTeamDeath();


        if (PlayerData.TryGet(Runner.LocalPlayer, out var d))
        {
                _uiManager.SetScore(d._score);
            if (GameTime.RemainingTime(Runner) <= _gameDuration - 30)
            {
                UpdateDangerSign(d);
            }
        }

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

            if (GameSoundManager.instance._GameBGMDESC._volum == 0)
            {
                GameSoundManager.instance._GameBGMDESC._stop();
            }

            GameSoundManager.instance._NowOrNeverDESC._unpause();
            _uiManager.OpenLastMinLeftWnd();
        }

        if (GameTime.RemainingTime(Runner) <= 11)
        {
            _uiManager.OpenCountDownWnd();
        }

        if (GameTime.RemainingTime(Runner) <= 1 && !_resultSequenceStarted)
        {
            _resultSequenceStarted = true;
            _gameEnd = true;

            GameSoundManager.instance.PlayerSFX(PlayerSFXName.whistleCmp00);

            _uiManager.StartFinishAnim();
            _uiManager.CloseUI();
            StartCoroutine(LoadResultSceneRoutine());
        }

        if (_isResultStarted && !_wasResultStarted)
        {
            _uiManager.EndFinishAnim();
            ResultManager._instance.StartTimeline();

            _wasResultStarted = true;
        }
    }

    void SpawnSPObj(GameObject root)
    {
        GameObject prefabTeam1 = Resources.Load<GameObject>("Object/RespawnPlatform_Team1");
        GameObject prefabTeam2 = Resources.Load<GameObject>("Object/RespawnPlatform_Team2");

        if (prefabTeam1 == null || prefabTeam2 == null)
        {
            Debug.LogError("SpawnSPObj: RespawnPlatform 프리팹을 Resources에서 찾지 못했습니다.");
            return;
        }

        _spawnPlatforms = new SpawnPlatform[2];

        _spawnPlatformTeam1 = Instantiate(prefabTeam1, root.transform.GetChild(0));
        _spawnRoot1 = _spawnPlatformTeam1.transform.GetChild(1).gameObject;
        _spawnPlatforms[0] = _spawnPlatformTeam1.GetComponent<SpawnPlatform>();

        _spawnPlatformTeam2 = Instantiate(prefabTeam2, root.transform.GetChild(1));
        _spawnRoot2 = _spawnPlatformTeam2.transform.GetChild(1).gameObject;
        _spawnPlatforms[1] = _spawnPlatformTeam2.GetComponent<SpawnPlatform>();
    }

    void SetCameraPos()
    {
        _team1Dolly.transform.SetParent(_spawnPlatformTeam1.transform.GetChild((int)CameraRootState.Dolly), false);
        _team1Fix.transform.SetParent(_spawnPlatformTeam1.transform.GetChild((int)CameraRootState.Fixed), false);

        _team2Dolly.transform.SetParent(_spawnPlatformTeam2.transform.GetChild((int)CameraRootState.Dolly), false);
        _team2Fix.transform.SetParent(_spawnPlatformTeam2.transform.GetChild((int)CameraRootState.Fixed), false);
    }

    // 호스트/클라이언트가 각자 로컬 시점(Awake)에 재생을 시작하는 대신,
    // 네트워크 동기 시간(Runner.SimulationTime) 기준 경과 위치로 딱 한 번만 seek한 뒤 Play()한다.
    // 이후 재생은 Unity Timeline의 정상 자동 재생 흐름을 타므로 Signal Emitter 등이 원래대로 발동한다.
    void StartIntroIfNeeded()
    {
        if (_introPlaying || !_introStarted) return;

        ResolveIntroDirector();
        if (!_introDirectorResolved) return;

        GameSoundManager.instance.OpeningBGM(_openingName);
        GameSoundManager.instance._UiBGMDESC._volum = 1;

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

    void ResolveIntroDirector()
    {
        if(_introDirectorResolved) return;
        if (!PlayerData.TryGet(Runner.LocalPlayer, out var d)) return;

        _playableDirector.playableAsset = (d._teamIndex == 1) ? _introTeam1 : _introTeam2;
        _introDirectorResolved = true;
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
            _self = player,
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
            onBeforeSpawned: (r, obj) =>
            {
                obj.GetComponent<NetworkPlayer>().SetSpawnIndex(spawnIndex);
                r.SetPlayerObject(player, obj);
            });
    }

    // 각 클라이언트가 RPC로 보낸 커스터마이징 값을 해당 플레이어의 PlayerData에 반영한다 (StateAuthority에서만 호출됨)
    public void SetPlayerCustomization(PlayerRef player, PlayerCustomization custom)
    {
        if (!PlayerData.TryGet(player, out var data)) return;

        data._custom = custom;
        PlayerData.Set(player, data);
    }

    // 각 클라이언트가 RPC로 보낸 닉네임을 해당 플레이어의 PlayerData에 반영한다 (StateAuthority에서만 호출됨)
    public void SetPlayerNickname(PlayerRef player, NetworkString<_32> nickname)
    {
        if (!PlayerData.TryGet(player, out var data)) return;

        data._nickName = nickname;
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

    public void PlayerKilled(PlayerRef killerPlayerRef, PlayerRef victimPlayerRef)
    {
        if (HasStateAuthority == false)
            return;

        if (PlayerData.TryGet(killerPlayerRef, out var data))
        {
            data._kills++;
            PlayerData.Set(killerPlayerRef, data);
        }

        RPC_InstantiateKillLog(killerPlayerRef, victimPlayerRef);
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

    void OnResultSceneLoadComplete()
    {
        if(ResultManager._instance == null) return;
        if (!PlayerData.TryGet(Runner.LocalPlayer, out var data)) return;

        _mainCamera.gameObject.SetActive(false);
        ResultManager._instance.OpenCameraNUI();

        Color teamColor = data._teamColor;
        Color enemyColor = data._enemyColor;

        float teamRate = data._teamIndex == 1 ? _team1InkRate : _team2InkRate;
        float enemyRate = teamRate == _team1InkRate ? _team2InkRate : _team1InkRate;


        ResultState teamState = teamRate > enemyRate ? ResultState.Win : ResultState.Lose;
        ResultState enemyState = teamRate > enemyRate ? ResultState.Lose : ResultState.Win;

        ResultManager._instance.InitResultResources(teamColor, enemyColor, teamRate, enemyRate , teamState, enemyState);
        SortPlayerScore();



        //_uiManager.EndFinishAnim();
        //ResultManager._instance.StartTimeline();
    }

    // 로컬 플레이어 시점에서 두 팀 잉크 비율을 비교해, 격차가 _dangerRateGap 이상이면
    // 비율이 낮은 팀에 위험 표시를 켠다. 게임이 진행 중일 때만 표시한다.
    void UpdateDangerSign(PlayerData localData)
    {
        bool myTeamDanger = false;
        bool enemyTeamDanger = false;

        if (_gameStart && !_gameEnd)
        {
            float myRate = localData._teamIndex == 1 ? _team1InkRate : _team2InkRate;
            float enemyRate = localData._teamIndex == 1 ? _team2InkRate : _team1InkRate;
            float gap = myRate - enemyRate;

            if (gap <= -_dangerRateGap) myTeamDanger = true;
            else if (gap >= _dangerRateGap) enemyTeamDanger = true;
        }

        if (myTeamDanger == _wasMyTeamDanger && enemyTeamDanger == _wasEnemyTeamDanger)
            return;

        _uiManager.SetDangerSign(myTeamDanger, enemyTeamDanger);
        _wasMyTeamDanger = myTeamDanger;
        _wasEnemyTeamDanger = enemyTeamDanger;
    }

    void CalculateColorRate()
    {
        if (_gameEnd) return;

        if (_rateCalcTimer.ExpiredOrNotRunning(Runner))
        {
            _rateCalcTimer = TickTimer.CreateFromSeconds(Runner, 0.2f);

            var rate = _gridManager.GetColorRate();
            _team1InkRate = GetCloseColorRate(rate, _teamColors1[_inkIdx]);
            _team2InkRate = GetCloseColorRate(rate, _teamColors2[_inkIdx]);
        }
    }

    float GetCloseColorRate(Dictionary<Color , float > colorrate, Color color)
    {
        float distToMyTeam = 0;
        float rate = 0;

        foreach (var kv in colorrate)
        {
            distToMyTeam = Mathf.Abs(kv.Key.r - color.r) + Mathf.Abs(kv.Key.g - color.g) + Mathf.Abs(kv.Key.b - color.b);
            
            if (distToMyTeam < 0.5f)
            {
                rate = kv.Value;
                break;
            }
        }

        return rate;
    }

    void CheckTeamDeath()
    {
        if(!_gameStart || _gameEnd) return;

        bool team1Dead = true;
        bool team2Dead = true;

        foreach (var kv in PlayerData)
        {
            PlayerData data = kv.Value;

            if (data._teamIndex == 1)
            {
                if (data._isAlive)
                {
                    team1Dead = false;
                }
            }
            else
            {
                if (data._isAlive)
                {
                    team2Dead = false;
                }
            }
        }

        if (team1Dead && !_wasTeam1Dead) _uiManager.PlayWipeOut();
        if (team2Dead && !_wasTeam2Dead) _uiManager.PlayWipeOut();
        _wasTeam1Dead = team1Dead;
        _wasTeam2Dead = team2Dead;
    }

    void FadeOutBGM()
    {
        if (_isIntroBGMEnd && !_introFadeOutComplete)
        {
            GameSoundManager.instance._UiBGMDESC._volum = Mathf.MoveTowards(GameSoundManager.instance._UiBGMDESC._volum, 0, _introFadeSpeed * Time.deltaTime);

            if (GameSoundManager.instance._UiBGMDESC._volum <= 0)
            {
                _introFadeOutComplete = true;
                GameSoundManager.instance._UiBGMDESC._volum = 1;
                GameSoundManager.instance._UiBGMDESC._stop();
            }
        }
    }

    void SetGameBGM()
    {
        musicType = Random.value < 0.5f
            ? MusicType.Normal
            : (MusicType)(1 + Random.Range(0, (int)MusicType.Count - 1));
        int fesIdx = Random.Range(0, 2);

        switch (musicType)
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
        GameSoundManager.instance._GameBGMDESC._volum = 0.7f;
        GameSoundManager.instance._NowOrNeverDESC._pause();
    }

    IEnumerator LoadResultSceneRoutine()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(SceneState.ResultScene.ToString(), LoadSceneMode.Additive);
        yield return op;

        if (ResultManager._instance != null)
        {
            ResultManager._instance.InitResultScene();

            yield return new WaitForSeconds(3);

            OnResultSceneLoadComplete();
        }
        else
        {
            Debug.LogError("ResultScene을 로드했지만 ResultManager 인스턴스를 찾지 못했습니다.");
        }

        yield return new WaitForSeconds(8f);

        if (HasStateAuthority)
        {
            _isResultStarted = true;
        }
    }

    void CreatGrid()
    {
        //if (WorldInkZoneManager._instance != null)
        //    _gridManager.CreateGride(WorldInkZoneManager._instance.XZWorldSize());

        if (MapManager._instance != null)
            _gridManager.CreateGride(MapManager._instance.XZWorldSize());
    }

    void SortPlayerScore()
    {
        if (!PlayerData.TryGet(Runner.LocalPlayer, out var localData)) return;

        int myTeamIndex = localData._teamIndex;

        List<PlayerData> teamList = new List<PlayerData>();
        List<PlayerData> enemyList = new List<PlayerData>();

        foreach (var kv in PlayerData)
        {
            PlayerData data = kv.Value;

            if (data._teamIndex == myTeamIndex)
                teamList.Add(data);
            else
                enemyList.Add(data);
        }

        teamList.Sort((a, b) => b._score.CompareTo(a._score));
        enemyList.Sort((a, b) => b._score.CompareTo(a._score));

        ResultManager._instance.GetPlayerData(teamList, enemyList, Runner.LocalPlayer);
    }

    Color GetTeamColor(int teamIndex) => teamIndex == 1 ? _teamColors1[_inkIdx] : _teamColors2[_inkIdx];
    Color GetEnemyColor(int teamIndex) => teamIndex == 1 ? _teamColors2[_inkIdx] : _teamColors1[_inkIdx];

    public bool PaintRadiusNode(Vector3 point, float radius, Color color)
    {
        return _gridManager.PaintNodesInRadius(point, radius, color);
    }

    public void CloseStartUI()
    {
        _uiManager.CloseStartUI();
        _nowTeamCam = true;
    }

    public void IntroBGMEnd()
    {
        _isIntroBGMEnd = true;
    }

    public void AddScore(PlayerRef shooter, int score)
    {
        if (!HasStateAuthority) return;
        if (!PlayerData.TryGet(shooter, out var data)) return;

        data._score += score;
        PlayerData.Set(shooter, data);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_InstantiateKillLog(PlayerRef killer, PlayerRef victim)
    {
        if (Runner.LocalPlayer != killer) return;

        string victimName = "";

        if (PlayerData.TryGet(victim, out PlayerData victimData))
        {
            victimName = victimData.DisplayName;
        }

        GameUIManager._instance.InstantiateKillLog(victimName);
    }

}

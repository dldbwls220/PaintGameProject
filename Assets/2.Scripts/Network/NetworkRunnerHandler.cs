using UnityEngine;
using Fusion;
using Fusion.Sockets;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Unity.VisualScripting;

public class NetworkRunnerHandler : MonoBehaviour
{
    public NetworkRunner _networkRunnerPrefab;

    [Header("Matchmaking")]
    [SerializeField] string _sessionPrefix = "PM_Room_";
    [SerializeField] int _maxPlayers = 8;
    [SerializeField] float _sessionListWaitSeconds = 2f;
    [SerializeField] float _connectTimeoutSeconds = 15f;
    [SerializeField] float _retryBaseDelay = 2f;   // 재시도 간격 (횟수마다 증가)
    [SerializeField] float _retryMaxDelay = 10f;  // 간격 상한
    [SerializeField] bool _autoReturnOnFatal = false; // 버전불일치 등에서 타이틀 자동 복귀

    [Header("Connection UI")]
    [SerializeField] GameObject _connectionWnd;
    [SerializeField] AnimationClip _closeConnectionClip;
    [SerializeField] GameObject[] _fadeWhileConnecting;
    [SerializeField] float _minConnectionWndSeconds = 0.5f;

    NetworkRunner _networkRunner;
    float[] _fadeOriginalAlpha;
    float _connectionWndShownTime;

    readonly List<SessionInfo> _sessions = new();
    System.Threading.CancellationTokenSource _cts;
    INetworkSceneManager _sceneManager;
    bool _resolved;
    int _retryCount;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //var clientTask = InitializeNetworkRunner(_networkRunner, GameMode.AutoHostOrClient, NetAddress.Any(), SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex), OnConnected);

        Debug.Log($"Server NetworkRunner Started");

        Spawner.SessionListUpdated += OnSessions;
        BeginConnect(forceHost: false);
    }

    private void OnDestroy() => Spawner.SessionListUpdated -= OnSessions;
    void OnSessions(List<SessionInfo> list) { _sessions.Clear(); _sessions.AddRange(list); }

    void BeginConnect(bool forceHost)
    {
        StopAllCoroutines();
        _resolved = false;
        _sessions.Clear();
        _cts?.Dispose();
        _cts = new System.Threading.CancellationTokenSource();

        if(_connectionWnd) _connectionWnd.SetActive(true);
        _connectionWndShownTime = Time.unscaledTime;
        HideFadeGroups();

        CleanupRunner();
        _networkRunner = Instantiate(_networkRunnerPrefab);
        _networkRunner.name = "Network Runner";
        _networkRunner.ProvideInput = true;

        _sceneManager = _networkRunner.GetComponents(typeof(MonoBehaviour)).OfType<INetworkSceneManager>().FirstOrDefault()?? _networkRunner.gameObject.AddComponent<NetworkSceneManagerDefault>();

        _ = ConnectFlow(forceHost);
    }

    async Task ConnectFlow(bool forceHost)
    {
        var runner = _networkRunner;

        // 1. 세션 로비 입장 → 방 목록 수신 시작
        var lobby = await runner.JoinSessionLobby(SessionLobby.ClientServer);
        if (!lobby.Ok) { OnConnectFailure(lobby.ShutdownReason); return; }

        // 2. 목록 채워질 시간 확보
        float t = 0f;
        while(t < _sessionListWaitSeconds) { t += Time.unscaledDeltaTime; await Task.Yield(); }

        // 3. 대상 결정
        GameMode mode;
        string sessionName;

        SessionInfo joinable = forceHost ? null : _sessions
            .Where(s => s.IsValid && s.IsOpen && s.IsVisible && s.PlayerCount < s.MaxPlayers)
            .OrderByDescending(s => s.PlayerCount) // 먼저 찬 방부터 채워 사람 모으기
            .FirstOrDefault();

        if(joinable != null)    { mode = GameMode.Client; sessionName = joinable.Name; }
        else                    { mode = GameMode.Host; sessionName = NextFreeRoomName(); }

        // 4. 접속
        _cts.CancelAfter(TimeSpan.FromSeconds(_connectTimeoutSeconds));
        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode = mode,
            SessionName = sessionName,
            PlayerCount = _maxPlayers,
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
            SceneManager = _sceneManager,
            StartGameCancellationToken = _cts.Token,
        });

        if(result.Ok)
        {
            OnConnected(runner);
        }
        else if (mode == GameMode.Client && (
       result.ShutdownReason == ShutdownReason.GameClosed ||
       result.ShutdownReason == ShutdownReason.GameIsFull ||
       result.ShutdownReason == ShutdownReason.GameNotFound))
        {
            // 고르는 사이에 그 방이 시작/꽉 참 → 내 방을 만든다
            BeginConnect(forceHost: true);
        }
        else
        {
            OnConnectFailure(result.ShutdownReason);
        }

    }

    void OnConnectFailure(ShutdownReason reason)
    {
        if (_resolved) return;
        _resolved = true;

        StopAllCoroutines();
        _cts?.Dispose(); _cts = null;
        RestoreFadeGroups();     // ★ 재시도 도중에도 로비가 안 잠기도록
        CleanupRunner();

        Debug.LogWarning($"[Connect] 실패: {reason} (재시도 {_retryCount})");

        // 재시도로 해결 불가능한 경우 → 루프 중단
        if (reason == ShutdownReason.IncompatibleConfiguration)
        {
            //if (_connectionFailedText) _connectionFailedText.text = "게임 버전이 일치하지 않습니다.";
            if (_autoReturnOnFatal) BackToTitle();
            return;
        }

        // 그 외 전부 자동 재시도 (백오프)
        _retryCount++;
        float delay = Mathf.Min(_retryBaseDelay * _retryCount, _retryMaxDelay);
        //if (_connectionFailedText) _connectionFailedText.text = $"접속 재시도 중... ({_retryCount})";
        StartCoroutine(RetryAfter(delay));
    }

    IEnumerator RetryAfter(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        BeginConnect(forceHost: false);   // 목록 다시 읽고 조인/호스트 자동 판정
    }

    public void BackToTitle()
    {
        _resolved = true;          // RetryAfter 코루틴이 다시 BeginConnect 못 하게
        StopAllCoroutines();
        _cts?.Dispose(); _cts = null;
        CleanupRunner();
        WipeTransitionManager.instance.LoadScene(DefineEnum.SceneState.StartScene);
    }

    static string DescribeReason(ShutdownReason reason) => reason switch
    {
        ShutdownReason.GameClosed => "이미 게임이 진행 중인 방입니다.\n새 방을 만들거나 잠시 후 다시 시도해 주세요.",
        ShutdownReason.GameIsFull => "방이 가득 찼습니다.\n새 방을 만들어 주세요.",
        ShutdownReason.GameNotFound => "방을 찾을 수 없습니다.",
        ShutdownReason.ServerInRoom => "방이 가득 찼습니다.",
        ShutdownReason.ConnectionRefused => "접속이 거부되었습니다.",
        ShutdownReason.ConnectionTimeout => "접속 시간이 초과되었습니다.",
        ShutdownReason.OperationCanceled => "접속 시간이 초과되었습니다.",   // ← CancelAfter 타임아웃이 이걸로 옴
        ShutdownReason.PhotonCloudTimeout => "서버에 연결하지 못했습니다.\n네트워크 상태를 확인해 주세요.",
        ShutdownReason.IncompatibleConfiguration => "게임 버전이 일치하지 않습니다.",
        ShutdownReason.Error => "알 수 없는 오류로 접속에 실패했습니다.",
        _ => $"접속에 실패했습니다. ({reason})",
    };

    string NextFreeRoomName()
    {
        var taken = new HashSet<string>(_sessions.Select(s => s.Name));
        for (int i = 1; i < 1000; i++)
            if (!taken.Contains(_sessionPrefix + i)) return _sessionPrefix + i;
        return _sessionPrefix + System.Guid.NewGuid().ToString("N").Substring(0, 6);
    }

    void CleanupRunner()
    {
        if (!_networkRunner) return;
        _networkRunner.Shutdown();
        Destroy(_networkRunner.gameObject);
        _networkRunner = null;
    }

    void OnConnected(NetworkRunner runner)
    {
        _resolved = true;
        _retryCount = 0;          // 성공했으니 재시도 카운터 리셋
        _cts?.Dispose(); _cts = null;
        StartCoroutine(HandleConnected());
    }

    IEnumerator HandleConnected()
    {
        // 재접속처럼 연결이 매우 빠르게 끝날 때 연결창이 한 프레임만 떴다 사라지지 않도록
        // 최소 표시 시간을 보장한다
        float elapsed = Time.unscaledTime - _connectionWndShownTime;
        if (elapsed < _minConnectionWndSeconds)
        {
            yield return new WaitForSecondsRealtime(_minConnectionWndSeconds - elapsed);
        }

        RestoreFadeGroups();

        if (_connectionWnd == null) yield break;

        yield return CloseConnectionWnd();
    }

    void HideFadeGroups()
    {
        if (_fadeWhileConnecting == null || _fadeWhileConnecting.Length == 0) return;

        _fadeOriginalAlpha = new float[_fadeWhileConnecting.Length];

        for (int i = 0; i < _fadeWhileConnecting.Length; i++)
        {
            if (_fadeWhileConnecting[i] == null) continue;

            CanvasGroup group = GetOrAddCanvasGroup(_fadeWhileConnecting[i]);
            _fadeOriginalAlpha[i] = group.alpha;

            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }
    }

    void RestoreFadeGroups()
    {
        if (_fadeWhileConnecting == null || _fadeOriginalAlpha == null) return;

        for (int i = 0; i < _fadeWhileConnecting.Length; i++)
        {
            if (_fadeWhileConnecting[i] == null) continue;

            CanvasGroup group = GetOrAddCanvasGroup(_fadeWhileConnecting[i]);

            group.alpha = _fadeOriginalAlpha[i];
            group.blocksRaycasts = true;
            group.interactable = true;
        }
    }

    CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        CanvasGroup group = target.GetComponent<CanvasGroup>();

        if (group == null)
        {
            group = target.AddComponent<CanvasGroup>();
        }

        return group;
    }

    IEnumerator CloseConnectionWnd()
    {
        Animation anim = _connectionWnd.GetComponent<Animation>();

        if (anim != null && _closeConnectionClip != null)
        {
            anim.clip = _closeConnectionClip;
            anim.Play();
            yield return new WaitForSeconds(_closeConnectionClip.length);
        }

        if (_connectionWnd != null)
        {
            _connectionWnd.SetActive(false);
        }
    }

    //protected virtual async Task InitializeNetworkRunner(NetworkRunner runner, GameMode gameMode, NetAddress address, SceneRef scene, Action<NetworkRunner> initialized)
    //{
    //    var sceneManager = runner.GetComponents(typeof(MonoBehaviour)).OfType<INetworkSceneManager>().FirstOrDefault();

    //    if (sceneManager == null)
    //    {
    //        //Handle networked objects that already exits in the scene
    //        sceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
    //    }

    //    runner.ProvideInput = true;

    //    var result = await runner.StartGame(new StartGameArgs
    //    {
    //        GameMode = gameMode,
    //        Address = address,
    //        Scene = scene,
    //        SessionName = "Port_Mackerel _GameScene",
    //        SceneManager = sceneManager
    //    });

    //    if (result.Ok)
    //    {
    //        // Fusion 1의 Initialized 콜백 대신 여기서 처리
    //        initialized?.Invoke(runner);
    //    }
    //    else
    //    {
    //        Debug.LogError($"StartGame 실패: {result.ShutdownReason}");
    //    }
    //}
}

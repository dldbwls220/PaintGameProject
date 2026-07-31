using UnityEngine;
using Fusion;
using Fusion.Sockets;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Threading.Tasks;
using System.Linq;

public class NetworkRunnerHandler : MonoBehaviour
{
    public NetworkRunner _networkRunnerPrefab;

    [Header("Connection UI")]
    [SerializeField] GameObject _connectionWnd;
    [SerializeField] AnimationClip _closeConnectionClip;
    [SerializeField] GameObject[] _fadeWhileConnecting;

    NetworkRunner _networkRunner;
    float[] _fadeOriginalAlpha;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (_connectionWnd != null)
        {
            _connectionWnd.SetActive(true);
        }

        HideFadeGroups();

        _networkRunner = Instantiate(_networkRunnerPrefab);
        _networkRunner.name = "Network Runner";

        var clientTask = InitializeNetworkRunner(_networkRunner, GameMode.AutoHostOrClient, NetAddress.Any(), SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex), OnConnected);

        Debug.Log($"Server NetworkRunner Started");
    }

    void OnConnected(NetworkRunner runner)
    {
        RestoreFadeGroups();

        if (_connectionWnd == null) return;

        StartCoroutine(CloseConnectionWnd());
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

    protected virtual async Task InitializeNetworkRunner(NetworkRunner runner, GameMode gameMode, NetAddress address, SceneRef scene, Action<NetworkRunner> initialized)
    {
        var sceneManager = runner.GetComponents(typeof(MonoBehaviour)).OfType<INetworkSceneManager>().FirstOrDefault();

        if (sceneManager == null)
        {
            //Handle networked objects that already exits in the scene
            sceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
        }

        runner.ProvideInput = true;

        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode = gameMode,
            Address = address,
            Scene = scene,
            SessionName = "Port_Mackerel _GameScene",
            SceneManager = sceneManager
        });

        if (result.Ok)
        {
            // Fusion 1의 Initialized 콜백 대신 여기서 처리
            initialized?.Invoke(runner);
        }
        else
        {
            Debug.LogError($"StartGame 실패: {result.ShutdownReason}");
        }
    }
}

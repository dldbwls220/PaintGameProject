using UnityEngine;
using Fusion;
using Fusion.Sockets;
using UnityEngine.SceneManagement;
using System;
using System.Threading.Tasks;
using System.Linq;

public class NetworkRunnerHandler : MonoBehaviour
{
    public NetworkRunner _networkRunnerPrefab;

    NetworkRunner _networkRunner;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _networkRunner = Instantiate(_networkRunnerPrefab);
        _networkRunner.name = "Network Runner";

        var clientTask = InitializeNetworkRunner(_networkRunner, GameMode.AutoHostOrClient, NetAddress.Any(), SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex), null);

        Debug.Log($"Server NetworkRunner Started");
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

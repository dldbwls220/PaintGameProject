using Fusion;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    // TODO: 임시 테스트용. 실제 강제시작 버튼 UI가 생기면 이 Update의 스페이스바 체크는 제거하고
    // 버튼 OnClick에서 ForceStart()를 직접 호출하도록 바꿀 것.
    [SerializeField] int _gameSceneBuildIndex = 1;

    NetworkRunner _runner;

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
        _runner.LoadScene(SceneRef.FromIndex(_gameSceneBuildIndex));
    }
}

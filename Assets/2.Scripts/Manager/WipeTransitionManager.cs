using DefineEnum;
using Fusion;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WipeTransitionManager : Singleton<WipeTransitionManager>
{
    [Header("WipeAnimation")]
    [SerializeField] Animation _anim;
    [SerializeField] AnimationClip[] _clips;

    [Header("SceneLoader")]
    public SceneState _gameSceneState = SceneState.LobbyScene;


    [Header("Loading")]
    [SerializeField] GameObject _loadingObj;
    [SerializeField] float _minLoadingDuration = 1.5f;

    NetworkRunner _networkrunner;
    bool _startRequested;

    public override void Awake()
    {
        base.Awake();
        CloseLoadingWnd();
    }

    public void LoadScene(SceneState state)
    {
        if (_startRequested) return;
        _startRequested = true;

        _gameSceneState = state;
        StartCoroutine(LoadingScene());
    }

    void StartWipeAnim()
    {
        _anim.clip = _clips[0];
        _anim.Play();
    }

    void EndWipeAnim()
    {
        _anim.clip = _clips[1];
        _anim.Play();
    }

    void OpendLodingWnd()
    {
        _loadingObj.SetActive(true);
    }

    void CloseLoadingWnd()
    {
        _loadingObj.SetActive(false);
    }

    void ForceStart()
    {
        if (_networkrunner == null)
        {
            _networkrunner = FindFirstObjectByType<NetworkRunner>();
        }

        if (_networkrunner == null || !_networkrunner.IsRunning)
        {
            Debug.LogWarning("ForceStart: NetworkRunner를 찾지 못했거나 아직 실행 중이 아닙니다.");
            return;
        }

        if (!_networkrunner.IsServer)
        {
            Debug.Log("ForceStart: 호스트만 게임을 시작할 수 있습니다.");
            return;
        }

        _networkrunner.SessionInfo.IsOpen = false;
        _networkrunner.SessionInfo.IsVisible = false;
        _networkrunner.LoadScene(SceneRef.FromIndex((int)_gameSceneState));
    }

    IEnumerator LoadingScene()
    {
        StartWipeAnim();
        yield return new WaitForSeconds(_anim.clip.length);

        OpendLodingWnd();
        float loadingStartTime = Time.time;
        ForceStart();

        // 호스트/클라이언트 공통: 이 피어의 로컬 씬 매니저가 실제로 로드를 끝낼 때까지 대기
        if (_networkrunner != null)
        {
            float waitForBusy = 0f;
            while (!_networkrunner.SceneManager.IsBusy && waitForBusy < 2f)
            {
                waitForBusy += Time.deltaTime;
                yield return null;
            }

            yield return new WaitUntil(() => !_networkrunner.SceneManager.IsBusy);
        }

        float remaining = _minLoadingDuration - (Time.time - loadingStartTime);
        if (remaining > 0f)
        {
            yield return new WaitForSeconds(remaining);
        }

        CloseLoadingWnd();
        EndWipeAnim();

        _startRequested = false;
    }
}

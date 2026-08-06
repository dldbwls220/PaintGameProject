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

    [Header("Fade Sound")]
    [SerializeField] float _fadeSpeed = 2f;

    NetworkRunner _networkrunner;
    bool _startRequested;
    bool _isCustomizeSceneLoaded;
    bool _isCustomizeSceneLoading;

    public override void Awake()
    {
        base.Awake();
        CloseLoadingWnd();
    }

    public void StartScene()
    {
        StartCoroutine(StartSceneAnim());
    }

    public void LoadScene(SceneState state)
    {
        if (_startRequested) return;
        _startRequested = true;

        _gameSceneState = state;
        StartCoroutine(LoadingScene());
    }

    public void OpenCustomizationScene()
    {
        if (_startRequested) return;

        //// 이미 로드되어 있으면 다시 로드하지 않고 껐다 켜기만 한다
        //if (_isCustomizeSceneLoaded)
        //{
        //    CustomizationUI._instance.transform.parent.gameObject.SetActive(true);
        //    return;
        //}

        if (_isCustomizeSceneLoading) return;

        StartCoroutine(LoadCustomizationScene());
    }

    public void CloseCustomizationScene()
    {
        //if (_isCustomizeSceneLoaded && CustomizationUI._instance != null)
        //{
        //    CustomizationUI._instance.transform.parent.gameObject.SetActive(false);
        //}

        StartCoroutine(CloseCustomizationWipeAnim());
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
            // 아직 네트워크 세션이 시작되지 않은 상태 (예: StartScene -> LobbyScene) - 로컬 씬 로드로 대체
            SceneManager.LoadSceneAsync((int)_gameSceneState);
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

    void FadeSound(bool isFadeIn)
    {
        StartCoroutine(FadeSoundRoutine(isFadeIn));
    }

    IEnumerator FadeSoundRoutine(bool isFadeIn)
    {
        if (!isFadeIn)
        {
            while (GameSoundManager.instance._UiBGMDESC._volum > 0)
            {
                GameSoundManager.instance._UiBGMDESC._volum = Mathf.MoveTowards(GameSoundManager.instance._UiBGMDESC._volum, 0, _fadeSpeed * Time.deltaTime);
                yield return null;
            }
        }
        else
        {
            while (GameSoundManager.instance._UiBGMDESC._volum < 1)
            {
                GameSoundManager.instance._UiBGMDESC._volum = Mathf.MoveTowards(GameSoundManager.instance._UiBGMDESC._volum, 1, _fadeSpeed * Time.deltaTime);
                yield return null;
            }
        }
      
    }

    IEnumerator LoadingScene()
    {
        FadeSound(false);

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
        _isCustomizeSceneLoaded = false;

        if (_gameSceneState == SceneState.LobbyScene)
        {
            GameSoundManager.instance.UIBGM(UIBGMName.Dubble_Bath);
            FadeSound(true);
        }
        else if (_gameSceneState == SceneState.Port_Mackerel_GameScene)
        {
            FadeSound(true);
        }
    }

    IEnumerator LoadCustomizationScene()
    {
        _isCustomizeSceneLoading = true;

        FadeSound(false);

        if (!_isCustomizeSceneLoaded)
        {
            StartWipeAnim();
            yield return new WaitForSeconds(_anim.clip.length);
            OpendLodingWnd();
            AsyncOperation op = SceneManager.LoadSceneAsync(SceneState.CustomizationScene.ToString(), LoadSceneMode.Additive);
            yield return op;
            _isCustomizeSceneLoading = false;
            _isCustomizeSceneLoaded = true;
            if (CustomizationUI._instance != null)
            {
                CustomizationUI._instance.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogError("CustomizationScene을 로드했지만 CustomizationUI 인스턴스를 찾지 못했습니다.");
            }

            CloseLoadingWnd();
            EndWipeAnim();
        }
        else
        {
            StartWipeAnim();
            yield return new WaitForSeconds(_anim.clip.length);
            _isCustomizeSceneLoading = false;
            CustomizationUI._instance.transform.parent.gameObject.SetActive(true);

            EndWipeAnim();
        }

        GameSoundManager.instance.UIBGM(UIBGMName.Dripping_with_Style);
        FadeSound(true);
    }

    IEnumerator CloseCustomizationWipeAnim()
    {
        _isCustomizeSceneLoading = true;

        FadeSound(false);

        StartWipeAnim();
        yield return new WaitForSeconds(_anim.clip.length);

        _isCustomizeSceneLoading = false;

        if (_isCustomizeSceneLoaded && CustomizationUI._instance != null)
        {
            CustomizationUI._instance.transform.parent.gameObject.SetActive(false);
        }

        EndWipeAnim();

        FadeSound(true);
        GameSoundManager.instance.UIBGM(UIBGMName.C_Side_Splattack);
    }

    IEnumerator StartSceneAnim()
    {
        OpendLodingWnd();
        yield return new WaitForSeconds(2f);
        CloseLoadingWnd();
        EndWipeAnim() ;
        GameSoundManager.instance.UIBGM(UIBGMName.C_Side_Splattack);
        GameSoundManager.instance._UiBGMDESC._loop = true;
    }
}

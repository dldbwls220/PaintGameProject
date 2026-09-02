using DefineEnum;
using DefineStructure;
using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class ResultManager : MonoBehaviour
{
    static ResultManager _uniqueinstance;
    public static ResultManager _instance => _uniqueinstance;

    [Header("Class References")]
    [SerializeField] ResultUI _resultUI;
    [Header("Animator")]
    [SerializeField] Animator _characterAnimator;
    [SerializeField] Animator _uiAnimator;
    [SerializeField] Animator[] _emoteAnims;
    [Header("JudgeNJudgeJr")]
    [SerializeField] JudgeObject _Judge;
    [SerializeField] JudgeObject _JudgeJr;
    [Header("Result Camera")]
    [SerializeField] Camera _camera;
    [Header("Timeline")]
    [SerializeField] PlayableDirector _resultTimeline;
    [Header("Set Emote Character")]
    [SerializeField] CharacterClothChanger[] _otherPlayer;
    [SerializeField] CharacterClothChanger _mePlayer;
    [SerializeField] EmoteCharacter[] _emoteCharacters;
    [SerializeField] TextMeshProUGUI[] _otherNickname;
    [SerializeField] TextMeshProUGUI _meNickname;
    [Header("Scene Select Timer")]
    [SerializeField] float _sceneChooseTimer = 30f;
    [Header("BGM Setting")]
    [SerializeField] float _resultFadeSpeed = 0.3f;

    PlayerRef _localPlayer;
    ResultBGM bgm;

    public float _teamRate { get; private set; }
    public float _enemyRate { get; private set; }

    public bool _isJudgingStart { get; private set; }
    public bool _isJudgingEnd { get; set; }

    bool _isResultSceneEnd;
    bool _wasResultSceneEnd;

    ResultState _myState;

    HashSet<EmoteState> _emotes;

    private void Awake()
    {
        _uniqueinstance = this;
    }

    private void Update()
    {
        FadeOutBGM();
    }

    public void InitResultScene()
    {
        _camera.gameObject.SetActive(false);
        _resultUI.CloseWnd();
        _emotes = new HashSet<EmoteState>();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void InitResultResources(Color team, Color enemy, float teamRate, float enemyRate, ResultState teamState, ResultState enemyState)
    {
        GetColor(team, enemy);
        GetTargetValueRate(teamRate, enemyRate);
        InitJudge(team, enemy, teamState, enemyState);
        _resultUI.InitSocreBoard(team, enemy, teamState);
        ChooseEmote(teamState);

        _myState = teamState;
        SetMusic();
    }

    public void GetPlayerData(List<PlayerData> teamdata, List<PlayerData> enemydata, PlayerRef player)
    {
        _localPlayer = player;
        InstantiateScoreBar(teamdata, enemydata);
        SetPlayer(teamdata);
    }


    public void OpenCameraNUI()
    {
        _camera.gameObject.SetActive(true);
        _resultUI.OpenWnd();
    }

    void InstantiateScoreBar(List<PlayerData> teamdata, List<PlayerData> enemydata)
    {
        _resultUI.InstantiateScoreBar(teamdata, enemydata);
    }

    void SetPlayer(List<PlayerData> teamdata)
    {
        int idx = 0;

        foreach (PlayerData playerdata in teamdata)
        {
            if (playerdata._self == _localPlayer)
            {
                _mePlayer.SetCustomization(playerdata._custom);
                _meNickname.text = playerdata.DisplayName;
            }
            else
            {
                _otherPlayer[idx++].SetCustomization(playerdata._custom);
                _otherNickname[idx].text = playerdata.DisplayName;
            }
        }

        for (int i = 0; i < _otherPlayer.Length; i++)
        {
            if (i < idx) continue;

            _otherPlayer[i].gameObject.SetActive(false);
            _otherNickname[i].text = null;
            Image image = _otherNickname[i].transform.GetComponentInParent<Image>();
            image.color = Color.clear;
        }

        foreach (var emote in _emoteCharacters)
        {
            emote.InitCharacter(teamdata[0]._teamColor);
        }
    }

    void GetColor(Color team, Color enemy)
    {
        _resultUI.InitUI(team, enemy);
    }

    void GetTargetValueRate(float team, float enemy)
    {
        _teamRate = team;
        _enemyRate = enemy;

        Debug.Log("ÆÀ : " + _teamRate + " Àû : " + _enemyRate);
    }

     void InitJudge(Color team, Color enemy, ResultState teamState, ResultState enemyState)
    {
        _Judge.InitJudge(team, teamState);
        _JudgeJr.InitJudge(enemy, enemyState);
    }

    void ChooseEmote(ResultState state)
    {
        
        while (_emotes.Count < 4)
        {
            EmoteState emoteState = state == ResultState.Win ? (EmoteState)Random.Range(1, (int)EmoteState.Count) : EmoteState.Loose;

            _emotes.Add(emoteState);
        }
    }


    #region[Timeline]
    public void JudgeNJudgeJrAppear()
    {
        _characterAnimator.SetTrigger("JudgeGoUp");
        _uiAnimator.SetTrigger("ResultUIGoUp");
    }

    public void PlayJudgeResultAnim()
    {
        _Judge.JudgeAnim(true);
        _JudgeJr.JudgeAnim(true);

        _isJudgingStart = true;

        GameSoundManager.instance.PlayerSFX(PlayerSFXName.Pour00);
        GameSoundManager.instance.PlayerSFX(PlayerSFXName.Pour10);
    }
    public void PlaySquidParticle()
    {
        _resultUI.PlaySquidParticle(_myState);
    }
    public void PlayResultBGM()
    {
        GameSoundManager.instance.Result(bgm);
    }

    public void JudgeNJudgeJrLeft()
    {
        _characterAnimator.SetTrigger("JudgeGoSide");
    }

    public void PlayEmotes()
    {
        int idx = 0;
        foreach (var emote in _emotes)
        {
            _emoteAnims[idx++].SetInteger("EmoteState", (int)emote);
        }
    }

    public void CharacterAppear()
    {
        _characterAnimator.SetTrigger("CharacterAppear");
        _uiAnimator.SetTrigger("UserNameUIAppear");
    }
   
    public void OpenResultBG()
    {
        switch (_myState)
        {
            case ResultState.Win:
                _resultUI.OpenWinBG();
                break;
            case ResultState.Lose:
                _resultUI.OpenLoseBG();
                break;
        }
    }

    public void CloseResultUI()
    {
        _characterAnimator.SetTrigger("MyCharacterZoomIn");
        _uiAnimator.SetTrigger("ResultSliderGoLeft");
    }

    public void OpenScoreBoard()
    {
        _uiAnimator.SetTrigger("OpenScoreWnd");
    }

    public void OpenSceneSelect()
    {
        _uiAnimator.SetTrigger("OpenSceneSelect");
    }

    #endregion[Timeline]



    void SetMusic()
    {
        switch (GameManager._instance.musicType)
        {
            case MusicType.Normal:
                bgm = _myState == ResultState.Win ? ResultBGM.Rinse_Repeat_NormalVictory : ResultBGM.Learning_Curve_NormalDefeat;
                break;
            case MusicType.SquidSisters:
                bgm = _myState == ResultState.Win ? ResultBGM.Inkopolis_Punch_SquidSistersVictory : ResultBGM.Stomping_Kick_SquidSistersDefeat;
                break;
            case MusicType.Tentacles:
                bgm = _myState == ResultState.Win ? ResultBGM.Fest_Zest_TentaclesVictory : ResultBGM.Partys_Over_TentaclesDefeat;
                break;
            case MusicType.DeepCut:
                bgm = _myState == ResultState.Win ? ResultBGM.EgoOverboard_DeepCutVictory : ResultBGM.Still_Swimmin_DeepCutDefeat;
                break;
        }

    }

    void FadeOutBGM()
    {
        if (_isResultSceneEnd && !_wasResultSceneEnd)
        {
            GameSoundManager.instance._ResultBGMDESC._volum = Mathf.MoveTowards(GameSoundManager.instance._ResultBGMDESC._volum, 0, _resultFadeSpeed * Time.deltaTime);

            if (GameSoundManager.instance._ResultBGMDESC._volum <= 0)
            {
                _wasResultSceneEnd = true;
                GameSoundManager.instance._ResultBGMDESC._volum = 0.7f;
                GameSoundManager.instance._ResultBGMDESC._stop();
            }
        }
    }

    public void StartTimeline()
    {
        _resultTimeline.Play();
    }

    public void NetworkShutdown()
    {
        FindFirstObjectByType<NetworkRunner>()?.Shutdown();
    }

    public void ReturnToStartScene()
    {
        NetworkShutdown();
        _isResultSceneEnd = true;
        WipeTransitionManager.instance.LoadScene(SceneState.StartScene);
    }

    public void ReturnToLobby()
    {
        NetworkShutdown();
        _isResultSceneEnd = true;
        WipeTransitionManager.instance.LoadScene(SceneState.LobbyScene);
    }

}

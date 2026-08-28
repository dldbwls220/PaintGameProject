using DefineEnum;
using DefineStructure;
using Fusion;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;

public class ResultManager : MonoBehaviour
{
    static ResultManager _uniqueinstance;
    public static ResultManager _instance => _uniqueinstance;

    [Header("Class References")]
    [SerializeField] ResultUI _resultUI;
    [Header("Animator")]
    [SerializeField] Animator _characterAnimator;
    [SerializeField] Animator _uiAnimator;
    [Header("JudgeNJudgeJr")]
    [SerializeField] JudgeObject _Judge;
    [SerializeField] JudgeObject _JudgeJr;
    [Header("Result Camera")]
    [SerializeField] Camera _camera;
    [Header("Timeline")]
    [SerializeField] PlayableDirector _resultTimeline;
    [Header("Set Cloth")]
    [SerializeField] CharacterClothChanger[] _otherPlayer;
    [SerializeField] CharacterClothChanger _mePlayer;

    PlayerRef _localPlayer;
    ResultBGM bgm;

    public float _teamRate { get; private set; }
    public float _enemyRate { get; private set; }

    public bool _isJudgingStart { get; private set; }
    public bool _isJudgingEnd { get; set; }

    ResultState _myState;

    private void Awake()
    {
        _uniqueinstance = this;
    }

    public void InitResultScene()
    {
        _camera.gameObject.SetActive(false);
        _resultUI.CloseWnd();
    }

    public void InitResultResources(Color team, Color enemy, float teamRate, float enemyRate, ResultState teamState, ResultState enemyState)
    {
        GetColor(team, enemy);
        GetTargetValueRate(teamRate, enemyRate);
        InitJudge(team, enemy, teamState, enemyState);
        _resultUI.InitSocreBoard(team, enemy, teamState);

        _myState = teamState;
        SetMusic();
    }

    public void GetPlayerData(List<PlayerData> teamdata, List<PlayerData> enemydata, PlayerRef player)
    {
        _localPlayer = player;
        InstantiateScoreBar(teamdata, enemydata);
        SetClothes(teamdata);
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

    void SetClothes(List<PlayerData> teamdata)
    {
        int idx = 0;

        foreach (PlayerData playerdata in teamdata)
        {
            if (playerdata._self == _localPlayer)
            {
                _mePlayer.SetCustomization(playerdata._custom);
            }
            else
            {
                _otherPlayer[idx].SetCustomization(playerdata._custom);
            }
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

   

    public void StartTimeline()
    {
        _resultTimeline.Play();
    }

}

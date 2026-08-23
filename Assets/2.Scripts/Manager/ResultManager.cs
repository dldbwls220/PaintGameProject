using DefineEnum;
using UnityEngine;
using UnityEngine.Playables;

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

        _myState = teamState;
    }

    public void OpenCameraNUI()
    {
        _camera.gameObject.SetActive(true);
        _resultUI.OpenWnd();
    }

    public void GetColor(Color team, Color enemy)
    {
        _resultUI.InitUI(team, enemy);
    }

    public void GetTargetValueRate(float team, float enemy)
    {
        _teamRate = team;
        _enemyRate = enemy;

        Debug.Log("ÆÀ : " + _teamRate + " Àû : " + _enemyRate);
    }

    public void InitJudge(Color team, Color enemy, ResultState teamState, ResultState enemyState)
    {
        _Judge.InitJudge(team, teamState);
        _JudgeJr.InitJudge(enemy, enemyState);
    }

    public void JudgeNJudgeJrAppear()
    {
        _characterAnimator.SetTrigger("JudgeGoUp");
        _uiAnimator.SetTrigger("ResultUIGoUp");
    }

    public void JudgeNJudgeJrLeft()
    {
        _characterAnimator.SetTrigger("JudgeGoSide");
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

    public void StartTimeline()
    {
        _resultTimeline.Play();
    }

}

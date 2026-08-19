using DefineEnum;
using UnityEngine;

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
    public float _teamRate { get; private set; }
    public float _enemyRate { get; private set; }

    public bool _isJudgingStart { get; private set; }
    public bool _isJudgingEnd { get; set; }

    private void Awake()
    {
        _uniqueinstance = this;
    }

    public void InitResultScene()
    {
        _camera.gameObject.SetActive(false);
        _resultUI.CloseWnd();
    }

    public void GetColor(Color team, Color enemy)
    {
        _resultUI.InitUI(team, enemy);
    }

    public void GetTargetValueRate(float team, float enemy)
    {
        _teamRate = team;
        _enemyRate = enemy;
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
    }

    public void FadeInScreen()
    {
        _resultUI.FadeInUI();
    }
}

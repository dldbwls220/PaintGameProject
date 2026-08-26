using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreBarUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _killScore;
    [SerializeField] TextMeshProUGUI _deathScore;
    [SerializeField] TextMeshProUGUI _score;
    [SerializeField] TextMeshProUGUI _name;
    [SerializeField] Image _killIcon;

    public void InitScoreBar(int kill, int death, int score, string name, Color color)
    {
        _killIcon.color = color;
        _killScore.text = "X " + kill.ToString();
        _deathScore.text = "X " + death.ToString();
        _name.text = name;
        _score.text = score.ToString()+"p";
    }
}

using UnityEngine;
using UnityEngine.UI;
using DefineEnum;
using DG.Tweening;
using Coffee.UIExtensions;
using TMPro;
using DefineStructure;
using System.Collections.Generic;

public class ResultUI : MonoBehaviour
{
    [Header("Slider")]
    [SerializeField] Slider _goodSlider;
    [SerializeField] Slider _badSlider;
    [SerializeField] float _slowSpeed;
    [SerializeField] float _fastSpeed;
    [SerializeField] TextMeshProUGUI _goodRate;
    [SerializeField] TextMeshProUGUI _enemyRate;

    [Header("Enemy Team Wave Scroll")]
    [SerializeField] RectTransform[] _enemyFrontWaves;
    [SerializeField] RectTransform[] _enemyBackWaves;
    [SerializeField] Image[] _enemyFrontWavesImage;
    [SerializeField] Image[] _enemyBackWavesImage;

    [Header("Good Team Wave Scroll")]
    [SerializeField] RectTransform[] _goodFrontWaves;
    [SerializeField] RectTransform[] _goodBackWaves;
    [SerializeField] Image[] _teamFrontWavesImage;
    [SerializeField] Image[] _teamBackWavesImage;

    [Header("Speed")]
    [SerializeField] float _frontSpeed = 30f;
    [SerializeField] float _backSpeed = 30f;

    [Header("ScoreBoard")]
    [SerializeField] GameObject _scoreBar;
    [SerializeField] Image _teamScoreBoardImg;
    [SerializeField] Image _enemyScoreBoardImg;
    [SerializeField] TextMeshProUGUI _winOrLoseTeam;
    [SerializeField] TextMeshProUGUI _winOrLoseEnemy;
    [SerializeField] GameObject _teamScoreContent;
    [SerializeField] GameObject _enemyScoreContent;

    [Header("UI CartoonSpark FX")]
    [SerializeField] RectTransform _FXRoot;
    [SerializeField] GameObject _cartoonSparkOutObj;
    UIParticle _cartoonSparkOut;
    RectTransform _cartoonFill;

    [Header("UI Squid FX")]
    [SerializeField] GameObject _winSquidObj;
    [SerializeField] GameObject _loseSquidObj;
    UIParticle _winSquid;
    UIParticle _loseSquid;

    [Header("UI Text")]
    [SerializeField] TextMeshProUGUI _goodText;
    [SerializeField] TextMeshProUGUI _badText;

    [Header("Result BG")]
    [SerializeField] GameObject _winBG;
    [SerializeField] GameObject _loseBG;
    [SerializeField] float _winBGAlpha = 0.3f;
    [SerializeField] float _fadeInSpeed = 2f;
    [SerializeField] Image _winStripe;
    bool _isWinBGOpen;
    bool _wasWinBGOpen;

    bool _wasJudgingStart;
    bool _wasJudgingEnd;

    void Start()
    {
        StartWaveScrollX(_enemyFrontWaves, _frontSpeed);
        StartWaveScrollX(_enemyBackWaves, -_backSpeed);
        StartWaveScrollX(_goodFrontWaves, _frontSpeed);
        StartWaveScrollX(_goodBackWaves, -_backSpeed);

        //InitUI(Color.red, Color.blue);
    }

    private void Update()
    {
        if (ResultManager._instance._isJudgingStart && !_wasJudgingStart)
        {
            IncreaseSliderValue25();
        }

        if (ResultManager._instance._isJudgingEnd || _wasJudgingEnd)
        {
            FinalSliderValue();
        }

        if (_isWinBGOpen && !_wasWinBGOpen)
        {
            if (!_wasWinBGOpen)
            {
                float alpha = _winStripe.color.a;

                alpha = Mathf.MoveTowards(_winStripe.color.a, _winBGAlpha, _fadeInSpeed * Time.deltaTime);

                _winStripe.color = new Color(_winStripe.color.r, _winStripe.color.g, _winStripe.color.b, alpha);

                if(_winStripe.color.a == _winBGAlpha) _wasWinBGOpen = true;
            }
        }
    }

    void LateUpdate()
    {
        SetParticlePos();
    }

    public void InitUI(Color team, Color enemy)
    {
        _winBG.SetActive(false);
        _loseBG.SetActive(false);

        _cartoonSparkOut = _cartoonSparkOutObj.GetComponent<UIParticle>();
        _cartoonFill = _cartoonSparkOutObj.GetComponent<RectTransform>();

        _winSquid = _winSquidObj.GetComponent<UIParticle>();
        _loseSquid = _loseSquidObj.GetComponent<UIParticle>();

        SetParticleColor(team, enemy);
        SetWaveColor(team, enemy);
        _badText.color = enemy;
        _goodText.color = team;
        _winStripe.color = new Color(team.r, team.g, team.b, 0);
    }

    void SetParticleColor(Color team, Color enemy)
    {
        ParticleSystem[] sparkParticles = _cartoonSparkOutObj.GetComponentsInChildren<ParticleSystem>();
        ParticleSystem[] winSquidParticles = _winSquidObj.GetComponentsInChildren<ParticleSystem>();

        for (int i = 0; i < sparkParticles.Length; i++)
        {
            var main = sparkParticles[i].main;
            switch (i)
            {
                case 0:
                    main.startColor = team;
                    break;
                case 2:
                    main.startColor = team;
                    break;
                case 4:
                    main.startColor = enemy;
                    break;
            }
        }

        foreach (var particle in winSquidParticles)
        {
            var main = particle.main;
            main.startColor= team;
        }
    }

    void SetWaveColor(Color team, Color enemy)
    {
        Color teambrighter = Color.Lerp(team, Color.white, 0.4f);
        Color enemybrighter = Color.Lerp(enemy, Color.white, 0.4f);

        foreach (Image img in _teamFrontWavesImage)
        {
            img.color = team;
        }

        foreach (Image img in _teamBackWavesImage)
        {
            img.color = teambrighter;
        }

        foreach (Image img in _enemyFrontWavesImage)
        {
            img.color = enemy;
        }

        foreach (Image img in _enemyBackWavesImage)
        {
            img.color = enemybrighter;
        }
    }

    void StartWaveScrollX(RectTransform[] waves, float speed)
    {
        foreach (var wave in waves)
        {
            float width = wave.rect.width;
            float distance = width * Mathf.Sign(speed);
            float duration = width / Mathf.Abs(speed);

            wave.DOAnchorPosX(distance, duration)
                .SetRelative(true)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart);
        }
    }

    void PlaySparkParticle()
    {
        if (_cartoonSparkOut != null)
        {
            _cartoonSparkOut.Play();
        }
    }

 

    void SetParticlePos()
    {
        if (_cartoonFill != null)
        {
            _cartoonFill.position = _FXRoot.position;
        }
    }

    void IncreaseSliderValue25()
    {
        _goodSlider.value = Mathf.MoveTowards(_goodSlider.value, 0.25f, _slowSpeed * Time.deltaTime);

        _badSlider.value = Mathf.MoveTowards(_badSlider.value, 0.25f, _slowSpeed * Time.deltaTime);

        if(_goodSlider.value == 0.25f) _wasJudgingStart = true;

        UpdateRateText(_goodSlider.value * 100, _badSlider.value * 100);
    }

    void FinalSliderValue()
    {
        float team = ResultManager._instance._teamRate + 0.02f;
        float enemy = ResultManager._instance._enemyRate;

        _goodSlider.value = Mathf.MoveTowards(_goodSlider.value, team, _fastSpeed * Time.deltaTime);

        _badSlider.value = Mathf.MoveTowards(_badSlider.value, enemy, _fastSpeed * Time.deltaTime);

        if (_goodSlider.value == team && _badSlider.value == enemy)
        {
            if (!_wasJudgingEnd)
            {
                GameSoundManager.instance.PlayerSFX(PlayerSFXName.DeadSplash00);
                PlaySparkParticle();
            }
            _wasJudgingEnd = true; 
        }

        UpdateRateText((_goodSlider.value - 0.02f) * 100 , _badSlider.value * 100);
    }

    void UpdateRateText(float team, float enemy)
    {
        _goodRate.text = $"{team:00.0}%";
        _enemyRate.text = $"{enemy:00.0}%";
    }

    public void InitSocreBoard(Color team, Color enemy, ResultState teamState)
    {
        _teamScoreBoardImg.color = team;
        _enemyScoreBoardImg.color = enemy;
        _winOrLoseTeam.color = team;
        _winOrLoseEnemy.color = enemy;

        switch (teamState)
        {
            case ResultState.Win:
                _winOrLoseTeam.text = "VICTORY";
                _winOrLoseEnemy.text = "DEFEAT";
                break;
            case ResultState.Lose:
                _winOrLoseTeam.text = "DEFEAT";
                _winOrLoseEnemy.text = "VICTORY";
                break;
        }
    }

    public void InstantiateScoreBar(List<PlayerData> teamData, List<PlayerData> enemyData)
    {
        // 중복 호출 시 스코어바가 누적되지 않도록 기존 항목을 먼저 제거한다.
        ClearScoreBar(_teamScoreContent.transform);
        ClearScoreBar(_enemyScoreContent.transform);

        if (teamData.Count > 0)
        {
            foreach (PlayerData playerData in teamData)
            {
                GameObject go = Instantiate(_scoreBar, _teamScoreContent.transform);
                ScoreBarUI ui = go.GetComponent<ScoreBarUI>();

                ui.InitScoreBar(playerData._kills, playerData._death, playerData._score, playerData.DisplayName, playerData._teamColor);
            }
        }
       
        if (enemyData.Count > 0)
        {
            foreach (PlayerData playerData in enemyData)
            {
                GameObject go = Instantiate(_scoreBar, _enemyScoreContent.transform);
                ScoreBarUI ui = go.GetComponent<ScoreBarUI>();

                ui.InitScoreBar(playerData._kills, playerData._death, playerData._score, playerData.DisplayName, playerData._teamColor);
            }
        }
    }

    void ClearScoreBar(Transform content)
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }
    }

    public void PlaySquidParticle(ResultState state)
    {
        switch (state)
        {
            case ResultState.Win:
                _winSquid.Play();
                break;
            case ResultState.Lose:
                _loseSquid.Play();
                break;
        }
    }

    public void OpenWinBG()
    {
        _winBG.SetActive(true);

        _isWinBGOpen = true;
    }

    public void OpenLoseBG()
    {
        _loseBG.SetActive(true);
    }

    public void OpenWnd()
    {
        gameObject.SetActive(true);
    }

    public void CloseWnd()
    {
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        foreach (var wave in _enemyFrontWaves) wave.DOKill();
        foreach (var wave in _enemyBackWaves) wave.DOKill();
        foreach (var wave in _goodFrontWaves) wave.DOKill();
        foreach (var wave in _goodBackWaves) wave.DOKill();
    }
}

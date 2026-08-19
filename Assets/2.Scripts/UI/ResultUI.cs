using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Coffee.UIExtensions;
using TMPro;

public class ResultUI : MonoBehaviour
{
    [Header("Slider")]
    [SerializeField] Slider _goodSlider;
    [SerializeField] Slider _badSlider;
    [SerializeField] float _slowSpeed;
    [SerializeField] float _fastSpeed;

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

    [Header("UI FX")]
    [SerializeField] RectTransform _FXRoot;
    [SerializeField] GameObject _cartoonSparkOutObj;
    UIParticle _cartoonSparkOut;
    RectTransform _cartoonFill;

    [Header("UI Text")]
    [SerializeField] TextMeshProUGUI _goodText;
    [SerializeField] TextMeshProUGUI _badText;

    [Header("UI References")]
    [SerializeField] Animation _fadeInAnim;

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
        if (Input.GetKey(KeyCode.E))
        {
            PlayParticle();
        }

        if (ResultManager._instance._isJudgingStart && !_wasJudgingStart)
        {
            IncreaseSliderValue25();
        }

        if (ResultManager._instance._isJudgingEnd || _wasJudgingEnd)
        {
            FinalSliderValue();
        }
    }

    void LateUpdate()
    {
        SetParticlePos();
    }

    public void InitUI(Color team, Color enemy)
    {
        _cartoonSparkOut = _cartoonSparkOutObj.GetComponent<UIParticle>();
        _cartoonFill = _cartoonSparkOutObj.GetComponent<RectTransform>();

        SetParticleColor(team, enemy);
        SetWaveColor(team, enemy);
        _badText.color = enemy;
        _goodText.color = team;
    }

    public void FadeInUI()
    {
        _fadeInAnim.Play();
    }

    void SetParticleColor(Color team, Color enemy)
    {
        ParticleSystem[] particles = _cartoonSparkOutObj.GetComponentsInChildren<ParticleSystem>();

        for (int i = 0; i < particles.Length; i++)
        {
            var main = particles[i].main;
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

    void PlayParticle()
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
                PlayParticle();

            _wasJudgingEnd = true; 
        }
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

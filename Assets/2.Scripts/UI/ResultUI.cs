using UnityEngine;
using DG.Tweening;
using Coffee.UIExtensions;

public class ResultUI : MonoBehaviour
{
    [Header("Enemy Team Wave Scroll")]
    [SerializeField] RectTransform[] _enemyFrontWaves;
    [SerializeField] RectTransform[] _enemyBackWaves;

    [Header("Good Team Wave Scroll")]
    [SerializeField] RectTransform[] _goodFrontWaves;
    [SerializeField] RectTransform[] _goodBackWaves;

    [Header("Fill Wave Scroll (Bottom to Top)")]
    [SerializeField] RectTransform[] _enemyFillWaves;
    [SerializeField] RectTransform[] _goodFillWaves;
    [SerializeField] float _fillSpeed = 30f;
    [SerializeField] float _fillWaveScrollDistance = 170f;

    [Header("Speed")]
    [SerializeField] float _frontSpeed = 30f;
    [SerializeField] float _backSpeed = 30f;

    [Header("Fill Movement Compensation")]
    [SerializeField] RectTransform _enemyFill;
    [SerializeField] RectTransform _goodFill;

    [Header("UI FX")]
    [SerializeField] RectTransform _FXRoot;
    [SerializeField] GameObject _cartoonSparkOut;

    Vector2 _enemyFillLastPos;
    Vector2 _goodFillLastPos;

    void Start()
    {
        StartWaveScrollX(_enemyFrontWaves, _frontSpeed);
        StartWaveScrollX(_enemyBackWaves, -_backSpeed);
        StartWaveScrollX(_goodFrontWaves, _frontSpeed);
        StartWaveScrollX(_goodBackWaves, -_backSpeed);

        StartWaveScrollY(_enemyFillWaves, _fillSpeed);
        StartWaveScrollY(_goodFillWaves, _fillSpeed);

        _enemyFillLastPos = _enemyFill.anchoredPosition;
        _goodFillLastPos = _goodFill.anchoredPosition;
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.E))
        {
            PlayParticle();
        }
    }

    void LateUpdate()
    {
        SetParticlePos();
        CompensateFillMovement(_enemyFill, ref _enemyFillLastPos, _enemyFrontWaves, _enemyBackWaves, _enemyFillWaves);
        CompensateFillMovement(_goodFill, ref _goodFillLastPos, _goodFrontWaves, _goodBackWaves, _goodFillWaves);
    }

    void CompensateFillMovement(RectTransform fill, ref Vector2 lastPos, RectTransform[] frontWaves, RectTransform[] backWaves, RectTransform[] fillWaves)
    {
        Vector2 delta = fill.anchoredPosition - lastPos;
        lastPos = fill.anchoredPosition;

        if (delta == Vector2.zero) return;

        foreach (var wave in frontWaves) wave.anchoredPosition -= delta;
        foreach (var wave in backWaves) wave.anchoredPosition -= delta;
        foreach (var wave in fillWaves) wave.anchoredPosition -= delta;
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

    void StartWaveScrollY(RectTransform[] waves, float speed)
    {
        foreach (var wave in waves)
        {
            float distance = _fillWaveScrollDistance * Mathf.Sign(speed);
            float duration = _fillWaveScrollDistance / Mathf.Abs(speed);

            wave.DOAnchorPosY(distance, duration)
                .SetRelative(true)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart);
        }
    }

    void PlayParticle()
    {        
        UIParticle particle = _cartoonSparkOut.GetComponent<UIParticle>();

        if (particle != null)
        {
            particle.Play();
        }       
    }

    void SetParticlePos()
    {
        RectTransform rect = _cartoonSparkOut.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.position = _FXRoot.position;
        }
    }

    void OnDestroy()
    {
        foreach (var wave in _enemyFrontWaves) wave.DOKill();
        foreach (var wave in _enemyBackWaves) wave.DOKill();
        foreach (var wave in _goodFrontWaves) wave.DOKill();
        foreach (var wave in _goodBackWaves) wave.DOKill();
        foreach (var wave in _enemyFillWaves) wave.DOKill();
        foreach (var wave in _goodFillWaves) wave.DOKill();
    }
}

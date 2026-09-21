using UnityEngine;
using Fusion;

public class NetworkBarrier : MonoBehaviour
{
    [Header("Barrier Setting")]
    [SerializeField] float _minBO = 0;
    [SerializeField] float _maxBO = 0.5f;
    [SerializeField] float _blinkDuration = 0.5f;
    [SerializeField] float _blinkSpeed = 30f;

    static readonly int BodyOpacityId = Shader.PropertyToID("_BodyOpacity");
    float _blinkTimer;

    public int _teamIdx;
    public int _hitCount;

    MaterialPropertyBlock _mpb;
    MeshRenderer _mesh;

    int _lastRenderedHitCount = -1;

    void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        _mesh = GetComponent<MeshRenderer>();
        _lastRenderedHitCount = _hitCount;
    }

    public void InitBarrier(int idx, Color color)
    {
        _teamIdx = idx;

        _mesh.GetPropertyBlock(_mpb);
        _mpb.SetColor("_FresnelColor", color);
        _mesh.SetPropertyBlock(_mpb);
    }

    public void IncreaseHitCount()
    {
        _hitCount++;
    }

    void BlinkBarrier()
    {
        if (_mesh == null) return;

        if (_hitCount != _lastRenderedHitCount)
        {
            _lastRenderedHitCount = _hitCount;
            _blinkTimer = _blinkDuration;
        }

        if (_blinkTimer <= 0f) return;

        _blinkTimer -= Time.deltaTime;

        float elapsed = _blinkDuration - Mathf.Max(_blinkTimer, 0f);
        float wave = Mathf.Sin(elapsed * _blinkSpeed) * 0.5f + 0.5f; // 0~1
        float fade = Mathf.Clamp01(_blinkTimer / _blinkDuration);    // 시간이 지날수록 약해짐
        float opacity = _blinkTimer > 0f ? Mathf.Lerp(_minBO, _maxBO, wave * fade) : _minBO;

        _mesh.GetPropertyBlock(_mpb);
        _mpb.SetFloat(BodyOpacityId, opacity);
        _mesh.SetPropertyBlock(_mpb);
    }
}

using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkInkProjectile : NetworkBehaviour
{
    [Header("Physics")]
    [SerializeField] private float _gravity = 20f;
    [SerializeField] private LayerMask _hitMask;

    [Header("Paint")]
    [SerializeField] private float _minRadius = 0.5f;
    [SerializeField] private float _maxRadius = 1.2f;
    [SerializeField] private float _hardness = 1f;
    [SerializeField] private float _strength = 1f;

    [Header("FX")]
    [SerializeField] private ParticleSystem _shootFX;
    [SerializeField] private GameObject _splashFXPrefab;
    [SerializeField] private GameObject _hitFXPrefab;

    [Networked] private InkProjectileData _data { get; set; }
    [Networked] private Color _inkColor { get; set; }
    [Networked] private int _finishedTick { get; set; }

    private bool _hitVisualPlayed = false;
    MaterialPropertyBlock _mpb;
    MaterialPropertyBlock _trailMpb;
    MeshRenderer _mesh;
    TrailRenderer _trailRenderer;
    ParticleSystem[] _splashParticle;

    // RPC 수신 보장을 위한 Despawn 지연: RPC 왕복 시간(~100ms) + 여유를 감안해 10틱
    private const int DESPAWN_DELAY_TICKS = 10;

    public void Initialize(Vector3 position, Vector3 velocity, Color inkColor)
    {
        _data = new InkProjectileData
        {
            Position = position,
            Velocity = velocity,
            FireTick = Runner.Tick,
            IsFinished = false
        };
        _inkColor = inkColor;
        
    }

    public override void Spawned()
    {
        _mpb = new MaterialPropertyBlock();
        _trailMpb = new MaterialPropertyBlock();
        _mesh = GetComponent<MeshRenderer>();
        _trailRenderer = GetComponent<TrailRenderer>();
        _splashParticle = GetComponentsInChildren<ParticleSystem>();

        _mesh.material.EnableKeyword("_EMISSION");
       

        if (_shootFX != null)
            _shootFX.Play();
    }

    public override void FixedUpdateNetwork()
    {
        if (_data.IsFinished)
        {
            // RPC가 모든 클라이언트에 도달할 시간을 확보한 뒤 Despawn
            if (HasStateAuthority && Runner.Tick >= _finishedTick + DESPAWN_DELAY_TICKS)
                Runner.Despawn(Object);
            return;
        }

        var previousPos = GetMovePosition(Runner.Tick - 1);
        var nextPos = GetMovePosition(Runner.Tick);

        var displacement = nextPos - previousPos;
        float distance = displacement.magnitude;
        if (distance <= 0f) return;

        Vector3 dir = displacement / distance;

        if (Physics.Raycast(previousPos, dir, out RaycastHit hit, distance, _hitMask))
        {
            if (HasStateAuthority)
                OnHit(hit);
            else if (!_hitVisualPlayed)
            {
                OnHitVisual(hit);    // 비-SA 클라이언트: 레이턴시 없이 즉시 FX 재생
                _hitVisualPlayed = true;
            }
            return;
        }

        if (nextPos.y < -20f && HasStateAuthority)
        {
            var data = _data;
            data.IsFinished = true;
            _data = data;
            _finishedTick = Runner.Tick;
        }
    }

    public override void Render()
    {
        if (_data.IsFinished) return;

        float renderTick = Runner.LocalRenderTime / Runner.DeltaTime;
        transform.position = GetMovePosition(renderTick);

        float time = (renderTick - _data.FireTick) * Runner.DeltaTime;
        Vector3 vel = _data.Velocity + new Vector3(0f, -_gravity, 0f) * time;
        if (vel.sqrMagnitude > 0.01f)
            transform.forward = vel.normalized;

        ApplyProjectileColor();
    }

    private void OnHitVisual(RaycastHit hit)
    {
        GameObject fxPrefab = hit.normal.y > 0.7f ? _splashFXPrefab : _hitFXPrefab;
        if (fxPrefab != null)
        {
            var fx = Instantiate(fxPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            ApplyColorToFX(fx);
            Destroy(fx, 3f);
        }
    }

    private void OnHit(RaycastHit hit)
    {
        var data = _data;
        data.IsFinished = true;
        data.ImpactPosition = hit.point;
        data.ImpactNormal = hit.normal;
        data.PaintRadius = Random.Range(_minRadius, _maxRadius);
        _data = data;
        _finishedTick = Runner.Tick;

        RPC_OnHit(hit.point, hit.normal, _inkColor, data.PaintRadius);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnHit(Vector3 point, Vector3 normal, Color color, float paintRadius)
    {
        // 페인팅: RenderTexture는 로컬이므로 모든 클라이언트에서 직접 호출 필요
        WorldInkManager.instance.Paint(point, color, paintRadius, _hardness);

        // FX: OnHitVisual로 이미 재생했으면 스킵 (중복 방지)
        if (!_hitVisualPlayed)
        {
            GameObject fxPrefab = normal.y > 0.7f ? _splashFXPrefab : _hitFXPrefab;
            if (fxPrefab != null)
            {
                var fx = Instantiate(fxPrefab, point, Quaternion.LookRotation(normal));
                ApplyColorToFX(fx);
                Destroy(fx, 3f);
            }
            _hitVisualPlayed = true;
        }
    }

    private Vector3 GetMovePosition(float tick)
    {
        float time = (tick - _data.FireTick) * Runner.DeltaTime;
        if (time <= 0f) return _data.Position;
        return _data.Position + _data.Velocity * time + new Vector3(0f, -_gravity, 0f) * (time * time * 0.5f);
    }

    void ApplyColorToFX(GameObject fx)
    {
        foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>())
        {
            var main = ps.main;
            main.startColor = _inkColor;
        }
    }

    void ApplyProjectileColor()
    {
        _mesh.GetPropertyBlock(_mpb);
        _mpb.SetColor("_BaseColor", _inkColor);
        _mpb.SetColor("_EmissionColor", _inkColor);
        _mesh.SetPropertyBlock(_mpb);

        _trailRenderer.GetPropertyBlock(_trailMpb);
        _trailMpb.SetColor("_BaseColor", _inkColor);
        _trailMpb.SetColor("_EmissionColor", _inkColor);
        _trailRenderer.SetPropertyBlock(_trailMpb);
    }
}

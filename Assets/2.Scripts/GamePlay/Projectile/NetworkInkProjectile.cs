using Fusion;
using UnityEngine;

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

    // StateAuthority(서버)에서 발사 직후 호출
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
        if (_shootFX != null)
            _shootFX.Play();
    }

    public override void FixedUpdateNetwork()
    {
        // IsFinished 상태가 되면 다음 틱에 StateAuthority가 디스폰
        if (_data.IsFinished)
        {
            if (HasStateAuthority)
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
                OnHit(hit, dir);     // 서버: 페인팅 판정 + RPC
            else
                OnHitVisual(hit);    // 클라이언트: FX만 즉시 재생 (RPC 대기 없음)
            return;
        }

       
        if (nextPos.y < -20f && HasStateAuthority)
        {
            var data = _data;
            data.IsFinished = true;
            _data = data;
        }
    }

    public override void Render()
    {
        if (_data.IsFinished) return;

        float renderTick = Runner.LocalRenderTime / Runner.DeltaTime;
        transform.position = GetMovePosition(renderTick);

        // 속도 방향으로 회전
        float time = (renderTick - _data.FireTick) * Runner.DeltaTime;
        Vector3 vel = _data.Velocity + new Vector3(0f, -_gravity, 0f) * time;
        if (vel.sqrMagnitude > 0.01f)
            transform.forward = vel.normalized;
    }

    //포물선 수식
    private Vector3 GetMovePosition(float tick)
    {
        float time = (tick - _data.FireTick) * Runner.DeltaTime;
        if (time <= 0f) return _data.Position;
        return _data.Position + _data.Velocity * time + new Vector3(0f, -_gravity, 0f) * (time * time * 0.5f);
    }

    // 클라이언트 전용: FX만 즉시 재생 (페인팅은 RPC로 처리)
    private void OnHitVisual(RaycastHit hit)
    {
        GameObject fxPrefab = hit.normal.y > 0.7f ? _splashFXPrefab : _hitFXPrefab;
        if (fxPrefab != null)
        {
            var fx = Instantiate(fxPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(fx, 3f);
        }
    }

    private void OnHit(RaycastHit hit, Vector3 direction)
    {
        var data = _data;
        data.IsFinished = true;
        data.ImpactPosition = hit.point;
        data.ImpactNormal = hit.normal;
        _data = data;

        float radius = Random.Range(_minRadius, _maxRadius);
        RPC_OnHit(hit.point, hit.normal, _inkColor, radius);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnHit(Vector3 point, Vector3 normal, Color color, float paintRadius)
    {
        // 페인팅은 서버 판정 기준으로 모든 클라이언트에 동기화
        WorldInkManager.instance.Paint(point, color, paintRadius, _hardness);

        // FX는 StateAuthority만 재생 (클라이언트는 OnHitVisual에서 이미 재생함)
        if (HasStateAuthority)
        {
            GameObject fxPrefab = normal.y > 0.7f ? _splashFXPrefab : _hitFXPrefab;
            if (fxPrefab != null)
            {
                var fx = Instantiate(fxPrefab, point, Quaternion.LookRotation(normal));
                Destroy(fx, 3f);
            }
        }
    }
}

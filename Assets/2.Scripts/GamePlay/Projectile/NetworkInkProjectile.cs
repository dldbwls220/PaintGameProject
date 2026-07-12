using Fusion;
using UnityEngine;

public class NetworkInkProjectile : NetworkBehaviour
{
    [Header("Physics")]
    [SerializeField] float _gravity = 20f;
    [SerializeField] float _straightDuration = 0.15f; // 이 시간 동안은 중력 무시하고 직선 이동, 이후 낙하 시작
    [SerializeField] LayerMask _hitMask;

    [Header("Paint")]
    [SerializeField] float _minRadius = 0.5f;
    [SerializeField] float _maxRadius = 1.2f;
    [SerializeField] float _hardness = 1f;
    [SerializeField] float _strength = 1f;

    [Header("FX")]
    [SerializeField] ParticleSystem _shootFX;
    [SerializeField] GameObject _splashFXPrefab;
    [SerializeField] GameObject _hitFXPrefab;

    [Networked] InkProjectileData _data { get; set; }
    [Networked] Color _inkColor { get; set; }
    [Networked] int _finishedTick { get; set; }
    [Networked] int _shooterTeam { get; set; }
    [Networked] float _damage { get; set; }

    bool _visualHidden;
    MaterialPropertyBlock _mpb;
    MaterialPropertyBlock _trailMpb;
    MeshRenderer _mesh;
    TrailRenderer _trailRenderer;
    ParticleSystem[] _splashParticle;

    // RPC 수신 보장을 위한 Despawn 지연: RPC 왕복 시간(~100ms) + 여유를 감안해 10틱
    const int DESPAWN_DELAY_TICKS = 10;

    public void Initialize(Vector3 position, Vector3 velocity, Color inkColor, float duration, int teamIndex, float damage)
    {
        _data = new InkProjectileData
        {
            Position = position,
            Velocity = velocity,
            FireTick = Runner.Tick,
            IsFinished = false
        };
        _inkColor = inkColor;
        _straightDuration = duration;
        _shooterTeam = teamIndex;
        _damage = damage;
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

        if (Runner.LagCompensation.Raycast(previousPos, dir, distance, Object.InputAuthority, out LagCompensatedHit lHit, _hitMask, HitOptions.IncludePhysX))
        {
            if (HasStateAuthority)
            {
                if (lHit.Hitbox != null)
                {
                    var hitOwner = lHit.GameObject.GetComponentInParent<NetworkInklingMovement>();

                    if (hitOwner != null && hitOwner._teamIndex == _shooterTeam)
                    {
                        Debug.Log("아군입니다");

                        return; // 아군이면 이번 틱은 무시 (필요하면 관통 처리)
                    }
                    else
                    {
                        OnHit(lHit.Point, lHit.Normal, true);
                        ApplyDamage(lHit.Hitbox);
                    }
                        
                }
                else
                    OnHit(lHit.Point, lHit.Normal, false);
            }
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
        if (_data.IsFinished)
        {
            HideVisual();
            return;
        }

        float renderTick = Runner.LocalRenderTime / Runner.DeltaTime;
        transform.position = GetMovePosition(renderTick);

        float time = (renderTick - _data.FireTick) * Runner.DeltaTime;
        Vector3 vel = GetVelocity(time);
        if (vel.sqrMagnitude > 0.01f)
            transform.forward = vel.normalized;

        ApplyProjectileColor();
    }

    void HideVisual()
    {
        if (_visualHidden) return;
        _visualHidden = true;

        _mesh.enabled = false;
        _trailRenderer.enabled = false;
    }

    void ApplyDamage(Hitbox enemy)
    {
        Health enemyHealth = enemy.Root.GetComponent<Health>();
        if (enemyHealth == null || !enemyHealth._isAlive) return;

        if (enemyHealth.ApplyDamage(Object.InputAuthority, _damage, DefineEnum.MainWeaponState.Shooter) == false) return;
    }

    void OnHit(Vector3 point, Vector3 normal, bool isEnemyHit)
    {
        var data = _data;
        data.IsFinished = true;
        data.ImpactPosition = point;
        data.ImpactNormal = normal;
        data.PaintRadius = Random.Range(_minRadius, _maxRadius);
        _data = data;
        _finishedTick = Runner.Tick;
        if (!isEnemyHit)
            RPC_OnHit(point, normal, _inkColor, data.PaintRadius);
        else
            RPC_OnEnemyHit(point, normal, _inkColor, data.PaintRadius);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_OnHit(Vector3 point, Vector3 normal, Color color, float paintRadius)
    {
        // 페인팅: RenderTexture는 로컬이므로 모든 클라이언트에서 직접 호출 필요
        WorldInkZoneManager.instance.PaintAuto(point, normal, color, paintRadius, _hardness);

        GameObject fxPrefab = _splashFXPrefab;
        if (fxPrefab != null)
        {
            var fx = Instantiate(fxPrefab, point, Quaternion.LookRotation(normal));
            ApplyColorToFX(fx);
            Destroy(fx, 3f);
        }

    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_OnEnemyHit(Vector3 point, Vector3 normal, Color color, float paintRadius)
    {
        GameObject fxPrefab = _hitFXPrefab;
        if (fxPrefab != null)
        {
            Vector3 spawnPos = point;
            if (Camera.main != null)
            {
                Vector3 dirToCam = (Camera.main.transform.position - point).normalized;
                spawnPos = point + dirToCam * 0.5f;
            }

            var fx = Instantiate(fxPrefab, spawnPos, Quaternion.identity);
            ApplyColorToFX(fx);
            Destroy(fx, 3f);
        }

    }

    Vector3 GetMovePosition(float tick)
    {
        float time = (tick - _data.FireTick) * Runner.DeltaTime;
        if (time <= 0f) return _data.Position;

        if (time <= _straightDuration)
            return _data.Position + _data.Velocity * time;

        // 직선 구간 종료 지점부터 낙하 포물선 시작 (위치/속도 연속)
        Vector3 straightEndPos = _data.Position + _data.Velocity * _straightDuration;
        float fallTime = time - _straightDuration;
        return straightEndPos + _data.Velocity * fallTime + new Vector3(0f, -_gravity, 0f) * (fallTime * fallTime * 0.5f);
    }

    Vector3 GetVelocity(float time)
    {
        if (time <= _straightDuration)
            return _data.Velocity;

        float fallTime = time - _straightDuration;
        return _data.Velocity + new Vector3(0f, -_gravity, 0f) * fallTime;
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

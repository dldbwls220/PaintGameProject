using DefineEnum;
using Fusion;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
public class NetworkInkProjectile : NetworkBehaviour
{
    [Header("Physics")]
    [SerializeField] float _gravity = 20f;
    [SerializeField] LayerMask _hitMask;

    [Header("Paint")]
    [SerializeField] float _minRadius = 0.5f;
    [SerializeField] float _maxRadius = 1.2f;
    [SerializeField] float _hardness = 1f;
    [SerializeField] float _strength = 1f;

    [Header("Detect Player")]
    [SerializeField] Vector3 _passBoxExtents = new Vector3(0.5f, 0.5f, 0.5f); // 로컬 x/y/z 절반 크기, z가 진행 방향
    [SerializeField] float _passBoxOffset = 0.5f; // 현재 위치에서 진행 반대 방향으로 박스 중심까지의 거리
    [SerializeField] LayerMask _passMask;

    [Header("FX")]
    [SerializeField] ParticleSystem _shootFX;
    [SerializeField] GameObject _splashFXPrefab;
    [SerializeField] GameObject _hitFXPrefab;

    [Header("SFX")]
    [SerializeField] AudioSource _sfx;
    [SerializeField] AudioClip _hitSFX;

    [Header("SFX3D")]
    [SerializeField] AudioSource _sfx3D;
    [SerializeField] AudioClip _passbySFX;

    [Networked] InkProjectileData _data { get; set; }
    [Networked] Color _inkColor { get; set; }
    [Networked] int _finishedTick { get; set; }
    [Networked] int _shooterTeam { get; set; }
    [Networked] float _damage { get; set; }
    [Networked] float _straightDistance { get; set; } // 이 거리(m)까지는 중력 무시하고 직선 이동, 이후 낙하 시작

    bool _visualHidden;
    MaterialPropertyBlock _mpb;
    MaterialPropertyBlock _trailMpb;
    MeshRenderer _mesh;
    TrailRenderer _trailRenderer;
    ParticleSystem[] _splashParticle;
    readonly HashSet<PlayerRef> _passByNotified = new HashSet<PlayerRef>();
    readonly List<LagCompensatedHit> _passHits = new List<LagCompensatedHit>();

    // RPC 수신 보장을 위한 Despawn 지연: RPC 왕복 시간(~100ms) + 여유를 감안해 10틱
    const int DESPAWN_DELAY_TICKS = 10;

    public void Initialize(Vector3 position, Vector3 velocity, Color inkColor, float straightDistance, int teamIndex, float damage, float gravity = 0)
    {
        _data = new InkProjectileData
        {
            Position = position,
            Velocity = velocity,
            FireTick = Runner.Tick,
            IsFinished = false
        };
        _inkColor = inkColor;
        _straightDistance = straightDistance;
        _shooterTeam = teamIndex;
        _damage = damage;

        if(gravity != 0)
            _gravity = gravity;
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
                // lHit.Hitbox는 IncludePhysX 옵션 때문에 같은 캐릭터를 맞혀도 PhysX 경로로 판정되면 null이 될 수 있어
                // (Fusion 문서: "Hitbox is null in case the hit was on PhysX"), Hitbox 유무 대신 GameObject로 대상을 판별한다.
                var hitOwner = lHit.GameObject != null ? lHit.GameObject.GetComponentInParent<NetworkInklingMovement>() : null;

                if (hitOwner != null)
                {
                    if (hitOwner._teamIndex == _shooterTeam)
                    {
                        Debug.Log($"[InkProjectile] 아군입니다 (target={lHit.GameObject.name}, teamIndex={hitOwner._teamIndex}, shooterTeam={_shooterTeam})");

                        return; // 아군이면 이번 틱은 무시 (필요하면 관통 처리)
                    }

                    OnHit(lHit.Point, lHit.Normal, true);
                    ApplyDamage(hitOwner);
                }
                else
                {
                    OnHit(lHit.Point, lHit.Normal, false);
                }
            }
        }

        if (nextPos.y < -20f && HasStateAuthority)
        {
            var data = _data;
            data.IsFinished = true;
            _data = data;
            _finishedTick = Runner.Tick;
        }

        if (!_data.IsFinished)
            CheckPlayerAround(nextPos, dir);
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

    void ApplyDamage(NetworkInklingMovement enemy)
    {
        Health enemyHealth = enemy.GetComponent<Health>();
        if (enemyHealth == null || !enemyHealth._isAlive) return;

        if (enemyHealth.ApplyDamage(Object.InputAuthority, _damage, MainWeaponState.Shooter) == false) return;
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

    void CheckPlayerAround(Vector3 position, Vector3 dir)
    {
        if (!HasStateAuthority) return;

        // 진행 방향 뒤쪽에 박스를 둔다
        Vector3 boxCenter = position - dir * _passBoxOffset;
        Quaternion boxRotation = Quaternion.LookRotation(dir);

        // 플레이어는 Fusion Hitbox로 판정되므로 일반 Physics.OverlapBox로는 감지되지 않는다
        Runner.LagCompensation.OverlapBox(boxCenter, _passBoxExtents, boxRotation, Object.InputAuthority, _passHits, _passMask, HitOptions.IncludePhysX);

        foreach (var hit in _passHits)
        {
            NetworkInklingMovement other = hit.GameObject != null ? hit.GameObject.GetComponentInParent<NetworkInklingMovement>() : null;

            if (other == null || other._teamIndex == _shooterTeam) continue;

            PlayerRef target = other.Object.InputAuthority;
            if (!_passByNotified.Add(target)) continue; // 같은 상대에게는 한 번만 알림

            RPC_OnEnemyPassBy(target);
        }
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
            _sfx.PlayOneShot(_hitSFX);
            Destroy(fx, 3f);
        }

    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_OnEnemyPassBy(PlayerRef target)
    {
        if (Runner.LocalPlayer != target) return; // 옆을 스쳐 지나간 그 플레이어에게만 재생

        _sfx3D.PlayOneShot(_passbySFX);
    }

    // 직선 구간 거리(_straightDistance)를 속력으로 환산한, 낙하가 시작되는 시각
    float GetStraightDuration()
    {
        float speed = _data.Velocity.magnitude;
        return speed > 0f ? _straightDistance / speed : 0f;
    }

    Vector3 GetMovePosition(float tick)
    {
        float time = (tick - _data.FireTick) * Runner.DeltaTime;
        if (time <= 0f) return _data.Position;

        float straightDuration = GetStraightDuration();
        if (time <= straightDuration)
            return _data.Position + _data.Velocity * time;

        // 직선 구간 종료 지점부터 낙하 포물선 시작 (위치/속도 연속)
        Vector3 straightEndPos = _data.Position + _data.Velocity * straightDuration;
        float fallTime = time - straightDuration;
        return straightEndPos + _data.Velocity * fallTime + new Vector3(0f, -_gravity, 0f) * (fallTime * fallTime * 0.5f);
    }

    Vector3 GetVelocity(float time)
    {
        float straightDuration = GetStraightDuration();
        if (time <= straightDuration)
            return _data.Velocity;

        float fallTime = time - straightDuration;
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

    private void OnDrawGizmos()
    {
        Vector3 dir = transform.forward;
        Vector3 boxCenter = transform.position - dir * _passBoxOffset;

        Gizmos.color = Color.cyan;
        Gizmos.matrix = Matrix4x4.TRS(boxCenter, Quaternion.LookRotation(dir), Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, _passBoxExtents * 2f);
    }
}

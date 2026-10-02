using DefineEnum;
using Fusion;
using System.Collections.Generic;
using UnityEngine;
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
    [SerializeField] float _hitVolume = 1f;
    [SerializeField] float _splashVolume = 0.5f;

    [Header("SFX3D")]
    [SerializeField] AudioSource _sfx3D;

    [Header("Scoreing")]
    [SerializeField] int _paintScore = 1;

    [Header("Mask")]
    [SerializeField] LayerMask _playerMask;
    [SerializeField] LayerMask _worldMask;

    [Networked] InkProjectileData _data { get; set; }
    [Networked] Color _inkColor { get; set; }
    [Networked] int _finishedTick { get; set; }
    [Networked] int _shooterTeam { get; set; }
    [Networked] float _damage { get; set; }
    [Networked] float _straightDistance { get; set; } // 이 거리(m)까지는 중력 무시하고 직선 이동, 이후 낙하 시작
    [Networked] float _netGravity { get; set; }

    bool _visualHidden;
    Color _appliedColor;
    MaterialPropertyBlock _mpb;
    MaterialPropertyBlock _trailMpb;
    MeshRenderer _mesh;
    TrailRenderer _trailRenderer;
    ParticleSystem[] _splashParticle;
    readonly HashSet<PlayerRef> _passByNotified = new HashSet<PlayerRef>();
    readonly List<LagCompensatedHit> _passHits = new List<LagCompensatedHit>();
    readonly List<LagCompensatedHit> _playerHits = new List<LagCompensatedHit>();

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

        //if(gravity != 0)
        //    _gravity = gravity;

        _netGravity = gravity != 0 ? gravity : _gravity;
    }

    public override void Spawned()
    {
        _mpb = new MaterialPropertyBlock();
        _trailMpb = new MaterialPropertyBlock();
        _mesh = GetComponent<MeshRenderer>();
        _trailRenderer = GetComponent<TrailRenderer>();
        _splashParticle = GetComponentsInChildren<ParticleSystem>();

        // 스폰 직후 실제 궤적 위치로 처음 옮겨가는 순간의 이동을 TrailRenderer가 선으로
        // 그려버리면, 클라이언트에서 "엉뚱한 곳에서 발사 지점으로 휙 날아오는" 잔상처럼
        // 보인다. 시작 위치로 맞춘 뒤 트레일을 비워서 그 잔상이 생기지 않게 한다.
        if (!HasStateAuthority)
            transform.position = GetMovePosition(Runner.Tick);

        _trailRenderer.Clear();

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
                bool hitWorld = Runner.LagCompensation.Raycast(previousPos, dir, distance, Object.InputAuthority, out LagCompensatedHit wHit, _worldMask, HitOptions.IncludePhysX);

                // 벽보다 앞에 있는 플레이어만 유효하므로 벽까지의 거리로 검사 구간을 자른다
                float playerCheckDistance = hitWorld ? wHit.Distance : distance;

                if (TryGetNearestEnemyHit(previousPos, dir, playerCheckDistance, out LagCompensatedHit pHit, out NetworkInklingMovement hitowner))
                {
                    OnHit(pHit.Point, pHit.Normal, true, pHit.Hitbox.gameObject.layer);
                    ApplyDamage(hitowner);
                }
                else if (hitWorld)
                {
                    // 아군만 걸렸거나 아무도 없으면 벽 충돌을 그대로 처리 (아군은 관통)
                    OnHit(wHit.Point, wHit.Normal, false, wHit.GameObject.layer);
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

        // 호스트는 Spawned()가 Initialize()보다 먼저 불려 그 시점엔 _inkColor가 비어 있다.
        // 그래서 Render에서 적용하되, 값이 실제로 바뀐 프레임에만 다시 칠한다.
        if (_appliedColor != _inkColor)
            ApplyProjectileColor();
    }

    // 가장 가까운 히트박스 하나만 보면, 그게 아군일 때 같은 틱 구간 안의 벽이나 뒤에 있는 적을 놓친다.
    // 구간에 걸리는 히트박스를 전부 받아 아군은 건너뛰고 가장 가까운 적만 고른다.
    bool TryGetNearestEnemyHit(Vector3 origin, Vector3 dir, float distance, out LagCompensatedHit enemyHit, out NetworkInklingMovement enemy)
    {
        enemyHit = default;
        enemy = null;

        Runner.LagCompensation.RaycastAll(origin, dir, distance, Object.InputAuthority, _playerHits, _playerMask, true, HitOptions.None);

        float nearest = float.MaxValue;

        foreach (var hit in _playerHits)
        {
            if (hit.Hitbox == null || hit.Distance >= nearest) continue;

            var owner = hit.Hitbox.GetComponentInParent<NetworkInklingMovement>();
            if (owner == null || owner._teamIndex == _shooterTeam) continue;

            nearest = hit.Distance;
            enemyHit = hit;
            enemy = owner;
        }

        return enemy != null;
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

    void OnHit(Vector3 point, Vector3 normal, bool isEnemyHit, int layer)
    {
        var data = _data;
        data.IsFinished = true;
        data.ImpactPosition = point;
        data.ImpactNormal = normal;
        data.PaintRadius = Random.Range(_minRadius, _maxRadius);
        _data = data;
        _finishedTick = Runner.Tick;
        if (!isEnemyHit)
        {
            RPC_PlayInkSplashSound(layer);
            RPC_OnHit(point, normal, _inkColor, data.PaintRadius, layer);
        }
        else
            RPC_OnEnemyHit(point, normal, _inkColor, data.PaintRadius);
    }

    readonly List<Paintabale> _paintablesInRadius = new List<Paintabale>();

    // hit 지점을 중심으로 반경(radius) 안에 걸치는 모든 Paintable을 찾는다.
    // 모서리처럼 물체 여러 개가 겹친 지점에서도 실제로 칠해진 대상이 스플래시 범위와 맞도록 한다.
    List<Paintabale> FindPaintablesInRadius(Vector3 point, float radius)
    {
        _paintablesInRadius.Clear();

        int mask = 1 << LayerMask.NameToLayer("Paintable");
        Collider[] cols = Physics.OverlapSphere(point, radius, mask);
        foreach (Collider col in cols)
        {
            Paintabale paintable = col.GetComponentInParent<Paintabale>();
            if (paintable != null && !_paintablesInRadius.Contains(paintable))
                _paintablesInRadius.Add(paintable);
        }

        return _paintablesInRadius;
    }

    void CheckPlayerAround(Vector3 position, Vector3 dir)
    {
        if (!HasStateAuthority) return;

        // 진행 방향 뒤쪽에 박스를 둔다
        Vector3 boxCenter = position - dir * _passBoxOffset;
        Quaternion boxRotation = Quaternion.LookRotation(dir);

        // 플레이어는 Fusion Hitbox로 판정되므로 일반 Physics.OverlapBox로는 감지되지 않는다
        Runner.LagCompensation.OverlapBox(boxCenter, _passBoxExtents, boxRotation, Object.InputAuthority, _passHits, _playerMask);

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
    void RPC_OnHit(Vector3 point, Vector3 normal, Color color, float paintRadius, int layer)
    {
        if (layer == LayerMask.NameToLayer("Paintable"))
        {
            //if (WorldInkZoneManager._instance != null)
            //    WorldInkZoneManager._instance.PaintAuto(point, normal, color, paintRadius, _hardness);

            // Paintabale은 MonoBehaviour라 RPC 인자로 넘길 수 없으므로,
            // 씬에 고정 배치된 오브젝트라는 점을 이용해 각 클라이언트가 hit point 주변을 재탐색한다.
            // 스플래시 반경(paintRadius) 안에 걸치는 모든 Paintable을 칠해야
            // 모서리 등 물체가 여러 개 겹친 지점에서도 실제로 칠해진 것과 보이는 것이 일치한다.
            if (PaintManager._instance != null)
            {
                foreach (Paintabale paintable in FindPaintablesInRadius(point, paintRadius))
                    PaintManager._instance.paint(paintable, point, paintRadius, _hardness, _strength, color);
            }

            // 그리드 페인트는 모든 피어가 로컬로 수행(결과 씬 색상 비율 계산에 사용).
            // 점수 가산은 호스트에서만, 실제 발사자(Object.InputAuthority) 기준으로 처리한다.
            bool paintedNew = GameManager._instance.PaintRadiusNode(point, paintRadius, color);

            if (HasStateAuthority && paintedNew)
            {
                GameManager._instance.AddScore(Object.InputAuthority, _paintScore);
            }
        }

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

            GameSoundManager.instance.ProjectileSFX(ProjectileSFXName.HitEffectiveCommon02, _sfx, _hitVolume);

            Destroy(fx, 3f);
        }

    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_OnEnemyPassBy(PlayerRef target)
    {
        if (Runner.LocalPlayer != target) return; // 옆을 스쳐 지나간 그 플레이어에게만 재생

        int rnd = Random.Range((int)ProjectileSFX3DName.Swish00, (int)ProjectileSFX3DName.Swish03 + 1);
        GameSoundManager.instance.ProjectileSFX3D((ProjectileSFX3DName)rnd, _sfx3D);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    void RPC_PlayInkSplashSound(int layer)
    {
        if (layer == LayerMask.NameToLayer("Paintable"))
        {
            int rnd = Random.Range((int)ProjectileSFXName.inkHit00, (int)ProjectileSFXName.inkHit07 + 1);

            GameSoundManager.instance.ProjectileSFX((ProjectileSFXName)rnd, volume: _splashVolume);
        }
        else if (layer == LayerMask.NameToLayer("Obstacle"))
        {
            int rnd = Random.Range((int)ProjectileSFXName.inkHitSplash00, (int)ProjectileSFXName.inkHitSplash03 + 1);

            GameSoundManager.instance.ProjectileSFX((ProjectileSFXName)rnd, volume: _splashVolume);
        }
    }

    // 직선 구간 거리(_straightDistance)를 속력으로 환산한, 낙하가 시작되는 시각
    float GetStraightDuration()
    {
        float speed = _data.Velocity.magnitude;
        return speed > 0f ? _straightDistance / speed : 0f;
    }

    Vector3 GetMovePosition(float tick)
    {
        // 발사 후 경과 시간(초)
        float time = (tick - _data.FireTick) * Runner.DeltaTime;
        if (time <= 0f) return _data.Position;

        // 직선 구간이 끝나는 시간
        float straightDuration = GetStraightDuration();
        if (time <= straightDuration)
            return _data.Position + _data.Velocity * time;

        // 직선 구간 종료 지점부터 낙하 포물선 시작
        Vector3 straightEndPos = _data.Position + _data.Velocity * straightDuration;

        // 낙하가 시작된 뒤 지난 시간
        float fallTime = time - straightDuration;
        return straightEndPos + _data.Velocity * fallTime + 
            new Vector3(0f, -_netGravity, 0f) * (fallTime * fallTime * 0.5f);
    }

    Vector3 GetVelocity(float time)
    {
        float straightDuration = GetStraightDuration();
        if (time <= straightDuration)
            return _data.Velocity;

        float fallTime = time - straightDuration;
        return _data.Velocity + new Vector3(0f, -_netGravity, 0f) * fallTime;
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

        _appliedColor = _inkColor;
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

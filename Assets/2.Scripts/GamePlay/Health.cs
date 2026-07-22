using DefineEnum;
using Fusion;
using System.Linq;
using UnityEngine;

public class Health : NetworkBehaviour
{
    [Header("Health Setting")]
    [SerializeField] float _maxHealth = 100;
    [SerializeField] float _hitDuration = 2f;
    [SerializeField] float _healthRegenSpeed = 10f;
    [SerializeField] float _preRespawnDelay = 0.5f; // 리스폰 소리가 먼저 재생되는 대기 시간
    [SerializeField] float _respawnTime = 1f;       // 대기 이후 이어지는 기존 리스폰 연출 시간

    [Header("Death Splash Setting")]
    [SerializeField] float _radius = 1.5f;
    [SerializeField] float _hardness = 0.9f;
    [SerializeField] float _minPaintRadius = 0.5f;
    [SerializeField] float _maxPaintRadius = 1.5f;
    [SerializeField] int _splashCount = 12;
    [SerializeField] float _splashCastRadius = 0.2f;
    [SerializeField] LayerMask _paintMask = ~0;

    [Header("Game Object")]
    [SerializeField] GameObject _deathSplashFX;

    [Header("Sound")]
    [SerializeField] AudioSource _voiceSFX3D;
    [SerializeField] AudioSource _otherSFX3D;

    public bool _isAlive => _currentHealth > 0;
    public bool _isFull => _currentHealth >= _maxHealth;
    public bool _isHit;
    bool _wasAlive = true;
    float _prevHealth;
    Color _inkColor = Color.white;

    [Networked] public float _currentHealth { get; private set; }
    [Networked] public TickTimer _hitTimer { get; set; }
    [Networked] TickTimer _healthTimer { get; set; }
    [Networked] TickTimer _preRespawnTimer { get; set; }
    [Networked] TickTimer _respawningTimer { get; set; }

    [Networked] public NetworkBool _pendingRespawn { get; set; } = false;
    [Networked] public NetworkBool _nowRespawing { get; set; } = false;

    const int DESPAWN_DELAY_TICKS = 10;

    public override void Spawned()
    {
        if (HasStateAuthority)
            _currentHealth = _maxHealth;
    }

    public override void FixedUpdateNetwork()
    {
        if(_hitTimer.Expired(Runner)) _isHit = false;

        if (_healthTimer.Expired(Runner) && !_isAlive && !_pendingRespawn)
        {
            _pendingRespawn = true;

            _preRespawnTimer = TickTimer.CreateFromSeconds(Runner, _preRespawnDelay);
        }

        if (_pendingRespawn && _preRespawnTimer.Expired(Runner))
        {
            _pendingRespawn = false;

            if(GameManager._instance.PlayerData.TryGet(Object.InputAuthority, out var data))
                data._isAlive = true;

            _respawningTimer = TickTimer.CreateFromSeconds(Runner, _respawnTime);

            _nowRespawing = true;

            _currentHealth = _maxHealth;
        }

        if (_respawningTimer.Expired(Runner))
        {
            _nowRespawing = false;
        }

        if (HasStateAuthority)
            AutoHealthRegen();
    }

    public bool ApplyDamage(PlayerRef player, float damage, MainWeaponState mw)
    {
        if(!HasStateAuthority) return false;

        if(_currentHealth <= 0) return false;

        _currentHealth -= damage;

        _isHit = true;

        _hitTimer = TickTimer.CreateFromSeconds(Runner, _hitDuration);

        if (_currentHealth <= 0f)
        {
            _currentHealth = 0f;

            if (GameManager._instance.PlayerData.TryGet(Object.InputAuthority, out var data))
                data._isAlive = false;

            ExplodePaint();
            Respawn();
            //킬로그 추가
        }

        return true;
    }

    public void AutoHealthRegen()
    {
        if (!_isHit && !_isFull && _isAlive)
        {
            _currentHealth = Mathf.MoveTowards(_currentHealth, _maxHealth, _healthRegenSpeed * Runner.DeltaTime);
        }
    }

    public void ApplyColorToFX(Color color)
    {
        _inkColor = color;
    }

    public void PlayDeadSplashEffect()
    {

        if (_wasAlive && !_isAlive)
        {
            GameObject fx = Instantiate(_deathSplashFX, transform.position + new Vector3(0, 0.5f, 0), Quaternion.identity);

            ParticleSystem[] ps = fx.GetComponentsInChildren<ParticleSystem>();

            foreach (ParticleSystem p in ps)
            {
                var main = p.main;
                main.startColor = _inkColor;
            }

            PlayDeadSound();

            Destroy(fx, 3f);
        }

        _wasAlive = _isAlive;
    }

    public void PlayHitSound()
    {
        if (_prevHealth > _currentHealth && _isAlive && HasInputAuthority)
        {
            int Rand = Random.Range((int)PlayerVoiceSFXName.Voice_SquidGirl_Damage_00, (int)PlayerVoiceSFXName.Voice_SquidGirl_Damage_07 + 1);

            GameSoundManager.instance.PlayerVoiceSFX((PlayerVoiceSFXName)Rand);
            GameSoundManager.instance.PlayerSFX(PlayerSFXName.Damage00);
        }

        _prevHealth = _currentHealth;
    }

    void Respawn()
    {
        float time = 0;

        if (GameManager._instance.PlayerData.TryGet(Object.InputAuthority, out var data))
            time = data._myRespawnTime;

        _healthTimer = TickTimer.CreateFromSeconds(Runner, time);        
    }

    void ExplodePaint()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        for (int i = 0; i < _splashCount; i++)
        {
            Vector3 dir = Random.onUnitSphere;

            if (Physics.SphereCast(origin, _splashCastRadius, dir, out RaycastHit hit, _radius, _paintMask))
            {
                float t = 1f - Mathf.Clamp01(hit.distance / _radius);
                float paintRadius = Mathf.Lerp(_minPaintRadius, _maxPaintRadius, t);

                RPC_OnDeathPaint(hit.point, hit.normal, _inkColor, paintRadius);
            }
        }
    }

    void PlayDeadSound()
    {
        int Rand = Random.Range((int)PlayerVoiceSFX3DName.Voice_SquidGirl_Dead_00, (int)PlayerVoiceSFX3DName.Voice_SquidGirl_Dead_04 + 1);
        GameSoundManager.instance.PlayerVoiceSFX3D((PlayerVoiceSFX3DName)Rand, _voiceSFX3D);

        _otherSFX3D.volume = 0.7f;
        GameSoundManager.instance.PlayerSFX3D(PlayerSFX3DName.DeadSplash00, _otherSFX3D);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_OnDeathPaint(Vector3 point, Vector3 normal, Color color, float paintRadius)
    {
        WorldInkZoneManager.instance.PaintAuto(point, normal, color, paintRadius, _hardness);
    }
}

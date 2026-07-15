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

    public bool _isAlive => _currentHealth > 0;
    public bool _isFull => _currentHealth >= _maxHealth;
    public bool _isImmortal;
    public bool _isHit;
    bool _wasAlive = true;
    Color _inkColor = Color.white;

    [Networked] public float _currentHealth { get; private set; }
    [Networked] public TickTimer _hitTimer { get; set; }

    const int DESPAWN_DELAY_TICKS = 10;

    public override void Spawned()
    {
        if (HasStateAuthority)
            _currentHealth = _maxHealth;
    }

    public override void FixedUpdateNetwork()
    {
        if(_hitTimer.Expired(Runner)) _isHit = false;

        if (HasStateAuthority)
            AutoHealthRegen();
    }

    public bool ApplyDamage(PlayerRef player, float damage, MainWeaponState mw)
    {
        if(!HasStateAuthority) return false;

        if(_currentHealth <= 0) return false;

        if(_isImmortal) return false;

        _currentHealth -= damage;

        _isHit = true;

        _hitTimer = TickTimer.CreateFromSeconds(Runner, _hitDuration);

        if (_currentHealth <= 0f)
        {
            _currentHealth = 0f;
            ExplodePaint();
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

            Destroy(fx, 3f);
        }

        _wasAlive = _isAlive;
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

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_OnDeathPaint(Vector3 point, Vector3 normal, Color color, float paintRadius)
    {
        WorldInkZoneManager.instance.PaintAuto(point, normal, color, paintRadius, _hardness);
    }
}

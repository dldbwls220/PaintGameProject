using DefineEnum;
using Fusion;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class Health : NetworkBehaviour
{
    [Header("Health Setting")]
    [SerializeField] float _maxHealth = 100;
    [SerializeField] float _hitDuration = 2f;
    [SerializeField] float _healthRegenSpeed = 10f;
    [SerializeField] float _preRespawnDelay = 0.5f; // 리스폰 소리가 먼저 재생되는 대기 시간
    [SerializeField] float _respawnTime = 1f;       // 대기 이후 이어지는 기존 리스폰 연출 시간

    [Header("Assist Setting")]
    [SerializeField] float _attackerExpireTime = 8f; // 공격자가 어시스트 대상으로 남아있는 시간

    [Header("Death Icon Setting")]
    [SerializeField] GameObject _killedIcon;
    [SerializeField] GameObject _assistIcont;
    [SerializeField] GameObject _deathIcon;
    [SerializeField] Image _killedImage;
    [SerializeField] Image _asssitImage;
    [SerializeField] Image _deathImage;
    [SerializeField] float _beatenWndDelay = 1.5f;


    [Header("Death Splash Setting")]
    [SerializeField] float _radius = 1.5f;
    [SerializeField] float _hardness = 0.9f;
    [SerializeField] float _strength = 0.9f;
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

    [Header("Camera")]
    [SerializeField] TPSCamera _localCamera;

    [Header("Damage Screen")]
    [SerializeField] FullScreenPassRendererFeature _damageScreenFeature;
    [SerializeField] float _minRadius = 0.5f;
    [SerializeField] float _maxRadius = 1f;
    [SerializeField] Material _damageScreenMat;

    public bool _isAlive => _currentHealth > 0;
    public bool _isFull => _currentHealth >= _maxHealth;
    public bool _isHit;
    bool _beatenWndOpened;
    bool _wasAlive = true;
    float _prevHealth;
    Color _inkColor = Color.white;

    Dictionary<PlayerRef, TickTimer> _attackerTimers = new Dictionary<PlayerRef, TickTimer>();
    List<PlayerRef> _expiredAttackerBuffer = new List<PlayerRef>();

    [Networked] public float _currentHealth { get; private set; }
    [Networked] public TickTimer _hitTimer { get; set; }
    [Networked] TickTimer _healthTimer { get; set; }
    [Networked] TickTimer _preRespawnTimer { get; set; }
    [Networked] TickTimer _respawningTimer { get; set; }
    [Networked] TickTimer _beatenWndTimer { get; set; }



    [Networked] public NetworkBool _pendingRespawn { get; set; } = false;
    [Networked] public NetworkBool _nowRespawing { get; set; } = false;

    // 0번: 킬러, 1~2번: 어시스트(최대 2명)
    [Networked, Capacity(3)] public NetworkArray<PlayerRef> _lastAttackers => default;

    const int DESPAWN_DELAY_TICKS = 10;

    public override void Spawned()
    {
        if (HasStateAuthority)
            _currentHealth = _maxHealth;

        _killedIcon.SetActive(false);
        _assistIcont.SetActive(false);
        _deathIcon.SetActive(false);

        if (HasInputAuthority && _damageScreenFeature != null)
        {
            _damageScreenFeature.passMaterial = _damageScreenMat;

            _damageScreenMat.SetFloat("_Vignette_Smoothness", 0.3f);
            _damageScreenMat.SetFloat("_Vignette_Darkening", 0.6f);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if(_hitTimer.Expired(Runner)) _isHit = false;

        PruneExpiredAttackers();

        CheckFallDeath();

        if (_healthTimer.Expired(Runner) && !_isAlive && !_pendingRespawn)
        {
            _pendingRespawn = true;

            _preRespawnTimer = TickTimer.CreateFromSeconds(Runner, _preRespawnDelay);
        }

        if (_pendingRespawn && _preRespawnTimer.Expired(Runner))
        {
            _pendingRespawn = false;

            if (HasStateAuthority && GameManager._instance.PlayerData.TryGet(Object.InputAuthority, out var data))
            {
                data._isAlive = true;
                GameManager._instance.PlayerData.Set(Object.InputAuthority, data);
            }

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

    public override void Render()
    {
        UpdateDeathIcons();

        if (!HasInputAuthority) return;

        if (_damageScreenFeature != null)
        {
            float ratio = _currentHealth / _maxHealth;
            _damageScreenMat.SetColor("_Tint", _inkColor);
            _damageScreenMat.SetFloat("_Vignette_Radius", Mathf.Lerp(_minRadius, _maxRadius, ratio));
        }

        bool isFallDeath = _lastAttackers[0] == PlayerRef.None;

        if (_isAlive)
        {
            GameUIManager._instance.CloseBeatenWnd();
            _localCamera.DisableKillCam();
            _beatenWndOpened = false;
            

            if (HasInputAuthority)
            {
                _localCamera.OnOffAudio(_isAlive);
            }

        }
        else if (!_beatenWndOpened && _beatenWndTimer.Expired(Runner) && !isFallDeath)
        {
            PlayerRef killer = _lastAttackers[0];

            string killerName = "";
            if (GameManager._instance.PlayerData.TryGet(killer, out var killerData))
            {
                killerName = killerData.DisplayName.ToString();
                if (Runner.TryGetPlayerObject(killer, out NetworkObject killerObj))
                {
                    _localCamera.EnableKillCam(killerObj.transform);
                }

                if (HasInputAuthority)
                {
                    _localCamera.OnOffAudio(_isAlive);
                }
            }

            GameUIManager._instance.OpenBeatenWnd(killerName);
            _beatenWndOpened = true;
        }

        
    }

    void UpdateDeathIcons()
    {
        if (_isAlive)
        {
            _killedIcon.SetActive(false);
            _assistIcont.SetActive(false);
            _deathIcon.SetActive(false);
            return;
        }

        PlayerRef local = Runner.LocalPlayer;

        bool isKiller = _lastAttackers[0] == local;
        bool isAssist = !isKiller && (_lastAttackers[1] == local || _lastAttackers[2] == local);

        _killedIcon.SetActive(isKiller);
        _assistIcont.SetActive(isAssist);
        _deathIcon.SetActive(!isKiller && !isAssist);
    }

    public bool ApplyDamage(PlayerRef player, float damage, MainWeaponState mw)
    {
        if(!HasStateAuthority) return false;

        if(_currentHealth <= 0) return false;

        _currentHealth -= damage;

        _isHit = true;

        AddAttackerList(player);

        _hitTimer = TickTimer.CreateFromSeconds(Runner, _hitDuration);

        if (_currentHealth <= 0f)
        {
            Die(player);
        }

        return true;
    }

    void Die(PlayerRef killer)
    {
        _currentHealth = 0f;

        if (GameManager._instance.PlayerData.TryGet(Object.InputAuthority, out var data))
        {
            data._death++;              
            data._isAlive = false;
            GameManager._instance.PlayerData.Set(Object.InputAuthority, data);
        }

        _beatenWndTimer = TickTimer.CreateFromSeconds(Runner, _beatenWndDelay);

        ExplodePaint();
        Respawn();
        RecordLastAttackers(killer);    

        if (killer != PlayerRef.None)
            GameManager._instance.PlayerKilled(killer, Object.InputAuthority);
    }

    void CheckFallDeath()
    {
        if (!HasStateAuthority || !_isAlive) return;
        if (_pendingRespawn || _nowRespawing) return;
        if (MapManager._instance == null) return;

        if (transform.position.y < MapManager._instance.GetDeathHeight())
            Die(PlayerRef.None);
    }

    void RecordLastAttackers(PlayerRef killer)
    {
        _lastAttackers.Set(0, killer);

        int slot = 1;

        foreach (var kv in _attackerTimers)
        {
            if (slot >= _lastAttackers.Length) break;
            if (kv.Key == killer) continue;

            _lastAttackers.Set(slot, kv.Key);
            slot++;
        }

        for (; slot < _lastAttackers.Length; slot++)
            _lastAttackers.Set(slot, PlayerRef.None);
    }

    public void AutoHealthRegen()
    {
        if (!_isHit && !_isFull && _isAlive)
        {
            _currentHealth = Mathf.MoveTowards(_currentHealth, _maxHealth, _healthRegenSpeed * Runner.DeltaTime);
        }
    }

    public void ApplyColorToFX(Color Enemy, Color Team)
    {
        _inkColor = Enemy;
        _deathImage.color = Team;
        _asssitImage.color = Team;
        _killedImage.color = Team;
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

    readonly List<Paintabale> _paintablesInRadius = new List<Paintabale>();

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

    void AddAttackerList(PlayerRef attacker)
    {
        _attackerTimers[attacker] = TickTimer.CreateFromSeconds(Runner, _attackerExpireTime);
    }

    void PruneExpiredAttackers()
    {
        if (_attackerTimers.Count == 0) return;

        _expiredAttackerBuffer.Clear();

        foreach (var kv in _attackerTimers)
        {
            if (kv.Value.Expired(Runner))
                _expiredAttackerBuffer.Add(kv.Key);
        }

        foreach (var attacker in _expiredAttackerBuffer)
            _attackerTimers.Remove(attacker);
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
        //if (WorldInkZoneManager._instance == null) return;
        //WorldInkZoneManager._instance.PaintAuto(point, normal, color, paintRadius, _hardness);

        if (PaintManager._instance != null)
        {
            foreach (Paintabale paintable in FindPaintablesInRadius(point, paintRadius))
                PaintManager._instance.paint(paintable, point, paintRadius, _hardness, _strength, color);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_OpenBeatenWnd(PlayerRef victim, string killerName)
    {
        if (Runner.LocalPlayer != victim) return;

        GameUIManager._instance.OpenBeatenWnd(killerName);
    }

    private void OnDisable()
    {
        _damageScreenMat.SetFloat("_Vignette_Smoothness", 0f);
        _damageScreenMat.SetFloat("_Vignette_Darkening", 0f);
    }
}

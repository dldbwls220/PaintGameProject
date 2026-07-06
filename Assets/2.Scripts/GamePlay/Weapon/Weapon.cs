using UnityEngine;
using Fusion;

public class Weapon : NetworkBehaviour
{
    [Header("Shoot Setup")]
    [SerializeField] WeaponManager _manager;
    [SerializeField] bool _isAutomatic = true;
    [SerializeField] float _damage = 35f;
    [SerializeField] float _shootRate = 0.1f;
    [SerializeField] float _shootSpeed = 30f;
    [SerializeField] float _dispersion = 0.5f;
    [SerializeField] private float _straightDuration = 0.15f;
    [SerializeField] LayerMask _hitMask;

    [Header("Ink Projectile Setup")]
    [SerializeField] NetworkObject _projectilePrefab;
    [SerializeField] float _inkUseRate = 15;

    [Header("Sound")]
    [SerializeField] AudioSource _shootSound;
    [SerializeField] AudioSource _emptySound;

    int _lastRenderedShotCount = -1;

    [Networked] private TickTimer _shootTimer { get; set; }
    [Networked] private NetworkBool _prevShootPressed { get; set; }
    [Networked] private int _shotCount { get; set; }

    public override void Spawned()
    {
        // 스폰 시점(늦게 접속한 클라이언트 포함)의 현재 값으로 맞춰서, 과거에 쐈던 발수만큼 소리가 몰아 재생되는 것을 방지
        _lastRenderedShotCount = _shotCount;
    }

    public bool Shoot(Transform inkRoot,Vector3 shootTarget, Color color, bool isShootPressed, bool isEmpty)
    {
        bool isNewPress = isShootPressed && !_prevShootPressed;
        _prevShootPressed = isShootPressed;

        if (isShootPressed == false) return false;

        // 단발: 새로 누른 순간에만 발사, 누르고 있어도 재발사 안 됨
        if (_isAutomatic == false && isNewPress == false) return false;

        if(_shootTimer.ExpiredOrNotRunning(Runner) == false) return false;

        if (isEmpty)
        {
            PlayEmptySound(isShootPressed);
            return false;
        }

        Random.InitState(Runner.Tick * unchecked((int)Object.Id.Raw));

        ShootProjectile(inkRoot, shootTarget, color);
        _shootTimer = TickTimer.CreateFromSeconds(Runner, _shootRate);

        UseInk();

        _shotCount++;

        return true;
    }

    public void ShootProjectile(Transform inkRoot,Vector3 shootTarget, Color color)
    {
        if (_projectilePrefab != null && inkRoot != null)
        {
            Vector3 shootDir = (shootTarget - inkRoot.position).normalized;
            Vector3 projectileDirection = shootDir;
            if (_dispersion > 0)
            {
                var dispersionRotation = Quaternion.Euler(Random.insideUnitSphere * _dispersion);
                projectileDirection = dispersionRotation * shootDir;
            }

            var team = GetComponentInParent<NetworkInklingMovement>()?._teamIndex ?? 0;

            var obj = Runner.Spawn(_projectilePrefab, inkRoot.position, Quaternion.LookRotation(projectileDirection), Object.InputAuthority);
            obj.GetComponent<NetworkInkProjectile>()?.Initialize(inkRoot.position, projectileDirection * _shootSpeed, color, _straightDuration, team);
        }
    }

    void UseInk()
    {
        _manager._currentInk = Mathf.MoveTowards(_manager._currentInk, 0, _inkUseRate * Runner.DeltaTime);

        float offsetSpeed = _inkUseRate * (0.5f / _manager._totalInk);
        _manager._inktankOffset = Mathf.MoveTowards(_manager._inktankOffset, 0.5f, offsetSpeed * Runner.DeltaTime);
    }   

    void PlayEmptySound(bool isShootPressed)
    {
        if(_emptySound == null || _emptySound.isPlaying) return;

        if (Runner.IsForward && HasInputAuthority)
        {
            _emptySound.Play();
        }
    }

    // NetworkInklingMovement.Render() -> WeaponManager.UpdateShootSound()에서 매 프레임 호출
    // 실제로 발사가 성공한 틱(_shotCount 증가)마다 딱 한 번씩만 소리/이펙트 재생
    public void UpdateShootSound()
    {
        if (_shotCount == _lastRenderedShotCount) return;

        _lastRenderedShotCount = _shotCount;

        _manager.PlayShootFX();

        if (_shootSound != null)
        {
            _shootSound.Play();
        }
    }
}

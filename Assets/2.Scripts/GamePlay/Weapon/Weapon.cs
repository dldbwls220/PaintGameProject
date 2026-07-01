using UnityEngine;
using Fusion;

public class Weapon : NetworkBehaviour
{
    [Header("Shoot Setup")]
    [SerializeField] bool _isAutomatic = true;
    [SerializeField] float _damage = 35f;
    [SerializeField] float _shootRate = 0.1f;
    [SerializeField] float _shootSpeed = 30f;
    [SerializeField] float _dispersion = 0.5f;
    [SerializeField] LayerMask _hitMask;

    [Header("Ink Projectile Setup")]
    [SerializeField] NetworkObject _projectilePrefab;
    [SerializeField] float _inkPerShoot = 15;
    [SerializeField] Transform _inkRoot;

    [Header("Sound")]
    [SerializeField] AudioSource _shootSound;
    [SerializeField] AudioSource _emptySound;

    [Networked] private TickTimer _shootTimer { get; set; }

    public void ShootProjectile(Vector3 aimTarget, Color color)
    {
        if (_shootTimer.ExpiredOrNotRunning(Runner))
        {
            _shootTimer = TickTimer.CreateFromSeconds(Runner, _shootRate);

            if (_projectilePrefab != null && _inkRoot != null)
            {
                Vector3 shootDir = (aimTarget - _inkRoot.position).normalized;
                Vector3 projectileDirection = shootDir;
                if (_dispersion > 0)
                {
                    var dispersionRotation = Quaternion.Euler(Random.insideUnitSphere * _dispersion);
                    projectileDirection = dispersionRotation * shootDir;
                }
               
                var obj = Runner.Spawn(_projectilePrefab, _inkRoot.position, Quaternion.LookRotation(projectileDirection), Object.InputAuthority);
                obj.GetComponent<NetworkInkProjectile>()?.Initialize(_inkRoot.position, projectileDirection * _shootSpeed, color);
            }
        }        
    }
}

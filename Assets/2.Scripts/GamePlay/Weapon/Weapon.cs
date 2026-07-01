using UnityEngine;
using Fusion;

public class Weapon : NetworkBehaviour
{
    [Header("Shoot Setup")]
    [SerializeField] bool _isAutomatic = true;
    [SerializeField] float _damage = 35f;
    [SerializeField] float _shootRate = 0.1f;
    [SerializeField] float _shootSpeed = 30f;
    [SerializeField] LayerMask _hitMask;

    [Header("Ink Projectile Setup")]
    [SerializeField] NetworkObject _projectilePrefab;
    [SerializeField] float _inkPerShoot = 15;
    [SerializeField] Transform _inkRoot;

    [Header("Sound")]
    [SerializeField] AudioSource _shootSound;
    [SerializeField] AudioSource _emptySound;

    void ShootProjectile(Vector3 aimTarget, Color color)
    {
        if (_projectilePrefab != null && _inkRoot != null)
        {
            Vector3 shootDir = (aimTarget - _inkRoot.position).normalized;
            var obj = Runner.Spawn(_projectilePrefab, _inkRoot.position, Quaternion.LookRotation(shootDir), Object.InputAuthority);
            obj.GetComponent<NetworkInkProjectile>()?.Initialize(_inkRoot.position, shootDir * _shootSpeed, color);
        }
    }
}

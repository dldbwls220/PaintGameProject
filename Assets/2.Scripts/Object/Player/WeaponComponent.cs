using System.Collections;
using UnityEngine;
using DefineEnum;

public class WeaponComponent : MonoBehaviour
{
    [SerializeField] InklingController _inkling;
    [SerializeField] InkProjectile _projectile; // test
    [SerializeField] GameObject _inkRoot;

    float _lastSootTime = 0;

    [SerializeField] float _dmg;
    [SerializeField] float _shootRate;
    [SerializeField] float _useInkRate;

    bool _triggered;
    bool _isEmpty;
    bool _soundOn;

    void Start()
    {        
        _projectile = GetComponent<InkProjectile>();

        _triggered = false;
    }

    public void ShootingPaint(bool Shooting)
    {
        if (_inkling._currentInk > 0)
        {
            _isEmpty = false;
        }

        if (_isEmpty) return;

        _triggered = Shooting;

        if (_triggered)
        {
            UseInk();

            if (!_soundOn)
            {
                _soundOn = true;
                StartCoroutine(ShootSound());
            }
            
            if (!_isEmpty && _triggered && Time.time > _lastSootTime + _shootRate)
            {
                _lastSootTime = Time.time;
                _projectile.Launch(_inkRoot.transform.position, transform.forward);
            }
        }
        else
        {
            _soundOn = false;
        }

       
    }

    void UseInk()
    {
        if (!_isEmpty)
        {
            _inkling._currentInk = Mathf.MoveTowards(_inkling._currentInk, 0, _useInkRate * Time.deltaTime);

            float offsetSpeed = _useInkRate * (0.5f / _inkling._maxInk);
            _inkling._inkOffset = Mathf.MoveTowards(_inkling._inkOffset, 0.5f, offsetSpeed * Time.deltaTime);

            if (_inkling._currentInk <= 0)
            {
                _inkling._currentInk = 0;
                _isEmpty = true;

                Debug.Log("À×Å© ºñ¾úÀ½");
            }
        }
    }

    IEnumerator ShootSound()
    {
        while (_triggered && !_isEmpty)
        {
            SoundManager._instance.PlaySFX(SFXName.Shtr_Shot_00);

            yield return new WaitForSeconds(_shootRate * 2);
        }

        yield return null;
    }
}

using System.Collections;
using UnityEngine;
using DefineEnum;

public class ShootingComponent : MonoBehaviour
{
    InklingController _inkling;
    InkProjectile _projectile;
    [SerializeField] GameObject _inkRoot;

    float _lastSootTime = 0;
    [SerializeField] float _shootRate;

    bool _triggered;
    bool _isEmpty;

    void Start()
    {
        _inkling = GetComponent<InklingController>();
        _projectile = GetComponent<InkProjectile>();

        _triggered = false;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!_isEmpty && _triggered && Time.time > _lastSootTime + _shootRate)
        {
            _lastSootTime = Time.time;
            _projectile.Launch(_inkRoot.transform.position, transform.forward);
        }
    }

    public void ShootingPaint(bool Shooting)
    {
        if (_inkling._currentInk > 0)
        {
            _isEmpty = false;
        }

        if (_isEmpty) return;

        if (!_triggered && Shooting)
        {
            _triggered = true;
            StartCoroutine(ShootSound());   
        }
        else if (_triggered && !Shooting)
        {            
            _triggered = false;
        }
    }

    public void UseInk(float inkRate)
    {
        if (_inkling._currentInk > 0)
        {
            _isEmpty = false;
        }

        if (!_isEmpty)
        {
            _inkling._currentInk = Mathf.MoveTowards(_inkling._currentInk, 0, inkRate * Time.deltaTime);

            float offsetSpeed = inkRate * (0.5f / _inkling._maxInk);
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

            yield return new WaitForSeconds(0.1f);
        }

        yield return null;
    }
}

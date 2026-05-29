using UnityEngine;

public class ShootingComponent : MonoBehaviour
{
    InklingController _inkling;
    [SerializeField] ParticleSystem _paintParticle;

    bool _triggered;
    bool _isEmpty;

    void Start()
    {
        _inkling = GetComponent<InklingController>();

        _triggered = false;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        
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
            _paintParticle.Play();
            _triggered = true;
        }
        else if (_triggered && !Shooting)
        {
            _paintParticle.Stop();
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
                _paintParticle.Stop();
                _isEmpty = true;

                Debug.Log("À×Å© ºñ¾úÀ½");
            }
        }
    }
}

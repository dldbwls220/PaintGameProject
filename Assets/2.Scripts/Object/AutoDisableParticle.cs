using UnityEngine;

public class AutoDisableParticle : MonoBehaviour
{
    ParticleSystem _particle;

    void Awake()
    {
        _particle = GetComponent<ParticleSystem>();
    }

    void OnEnable()
    {
        if (_particle != null)
        {
            _particle.Clear();
            _particle.Play();
        }
    }

    void Update()
    {
        if (_particle != null && !_particle.isPlaying)
        {
            gameObject.SetActive(false);
        }
    }
}

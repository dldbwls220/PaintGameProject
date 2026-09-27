using System.Collections.Generic;
using UnityEngine;

public class ParticleController : MonoBehaviour
{
    public Color _paintColor;

    [SerializeField] float _minRadius = 0.05f;
    [SerializeField] float _maxRadius = 0.2f;
    [SerializeField] float _strength = 1;
    [SerializeField] float _hardness = 1;
    [Space]
    ParticleSystem _part;
    List<ParticleCollisionEvent> _collisionEvents;

    void Start()
    {
        _part = GetComponent<ParticleSystem>();
        _collisionEvents = new List<ParticleCollisionEvent>();
        _paintColor = Color.navyBlue;
    }

    private void OnParticleCollision(GameObject other)
    {
        int numCollisionEvents = _part.GetCollisionEvents(other, _collisionEvents);

        Paintabale p = other.GetComponent<Paintabale>();
        if (p != null)
        {
            for (int i = 0; i < numCollisionEvents; i++)
            {
                Vector3 pos = _collisionEvents[i].intersection;
                float radius = Random.Range(_minRadius, _maxRadius);
                PaintManager._instance.paint(p, pos, radius, _hardness, _strength, _paintColor);
            }
        }
        Debug.Log("Painted");
    }
}

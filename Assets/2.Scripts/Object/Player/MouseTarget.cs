using UnityEngine;

public class MouseTarget : MonoBehaviour
{
    [SerializeField] LayerMask _playerLayer;
    [SerializeField] float _minDistance = 3f;
    [SerializeField] float _defaultDistance = 20f;
    [SerializeField] float _smoothSpeed = 15f;

    Vector3 _targetPosition;

    void Start()
    {
        _targetPosition = transform.position;
    }

    void Update()
    {
        if (Camera.main == null) return;
        GetMousePos();
    }
    void GetMousePos()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int mask = ~_playerLayer;

        Vector3 desiredPosition;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, mask) && hit.distance >= _minDistance)
        {
            desiredPosition = hit.point;
        }
        else
        {
            desiredPosition = ray.origin + ray.direction * _defaultDistance;
        }

        _targetPosition = Vector3.Lerp(_targetPosition, desiredPosition, _smoothSpeed * Time.deltaTime);
        transform.position = _targetPosition;
    }
}

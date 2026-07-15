using Unity.VisualScripting;
using UnityEngine;

public class MouseTarget : MonoBehaviour
{
    [SerializeField] LayerMask _playerLayer;
    [SerializeField] LayerMask _obstacleLayer = 0; // 총구 차단 체크용 (바닥/Paintable 제외, 벽 등 실제 장애물만)
    [SerializeField] float _minDistance = 3f;
    [SerializeField] float _maxDistance;
    [SerializeField] float _defaultDistance = 20f;
    [SerializeField] float _smoothSpeed = 15f;
    [SerializeField] float _muzzleSkinDistance = 0.5f; // 총구 바로 근처 자체 히트 무시용 여유 거리

    Vector3 _targetPosition;
    GameObject _targetObject;
    GameObject _aimRootObject;
    int _teamIdx;

    void Start()
    {
        _targetPosition = transform.position;

        if (_obstacleLayer.value == 0)
        {
            _obstacleLayer = LayerMask.GetMask("Obstacle", "Wall"); // 인스펙터에서 지정 안 하면 기본값
        }
    }

    void LateUpdate()
    {
        if (Camera.main == null) return;
        GetMousePos();
    }

    public void InitObj(GameObject targetObject, float distance, int teamIdx)
    {
        _targetObject = targetObject;
        _aimRootObject = _targetObject.transform.GetChild(1).gameObject;
        _maxDistance = distance;
        _teamIdx = teamIdx;
    }

    void GetMousePos()
    {
        //Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if(_targetObject == null) return;

        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);

        int mask = ~_playerLayer;

        Vector3 desiredPosition;

        if (Physics.Raycast(ray, out RaycastHit hit, _maxDistance, mask))
        {
            desiredPosition = hit.distance >= _minDistance ? hit.point : ray.origin + ray.direction * _minDistance;
            // 벽은 있지만 너무 가까움 → minDistance 지점에서 멈춤
        }
        else
        {
            desiredPosition = ray.origin + ray.direction * _defaultDistance; // 진짜 아무것도 안 맞음
        }

        // 카메라 시야에서는 장애물을 넘어 보여도, 총구 기준으로는 막혀 있을 수 있으므로
        // 총구 → 조준점 사이를 다시 검사해 더 가까운 충돌이 있으면 그 지점으로 당겨온다.
        Vector3 muzzlePosition = _aimRootObject.transform.position;
        Vector3 toDesired = desiredPosition - muzzlePosition;
        float distanceToDesired = toDesired.magnitude;

        if (distanceToDesired > _muzzleSkinDistance &&
            Physics.Raycast(muzzlePosition, toDesired / distanceToDesired, out RaycastHit muzzleHit, distanceToDesired, _obstacleLayer) &&
            muzzleHit.distance > _muzzleSkinDistance)
        {
            var hitOwner = muzzleHit.collider.gameObject.GetComponentInParent<NetworkInklingMovement>();
            bool isFriendly = hitOwner != null && hitOwner._teamIndex == _teamIdx;

            if (!isFriendly)
                desiredPosition = muzzleHit.point;
        }

        _targetPosition = Vector3.Lerp(_targetPosition, desiredPosition, _smoothSpeed * Time.deltaTime);
        transform.position = _targetPosition;
    }
}

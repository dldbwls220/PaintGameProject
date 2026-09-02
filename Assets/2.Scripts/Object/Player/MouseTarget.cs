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

    bool _enemyHit;
    bool _hitAnything;

    static readonly RaycastHit[] _muzzleHitBuffer = new RaycastHit[16];

    public bool _eHit { get { return _enemyHit; } }
    public bool _hA { get { return _hitAnything; } }

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
        _aimRootObject = targetObject.transform.GetChild(0).gameObject;
        _maxDistance = distance;
        _teamIdx = teamIdx;
    }

    void GetMousePos()
    {
        //Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if(_aimRootObject == null) return;

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

        bool muzzleBlocked = false;
        RaycastHit muzzleHit = default;
        NetworkInklingMovement muzzleOwner = null;

        if (distanceToDesired > _muzzleSkinDistance)
        {
            Vector3 muzzleDir = toDesired / distanceToDesired;
            int hitCount = Physics.RaycastNonAlloc(muzzlePosition, muzzleDir, _muzzleHitBuffer, distanceToDesired, _obstacleLayer);

            float nearest = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit h = _muzzleHitBuffer[i];
                if (h.distance <= _muzzleSkinDistance) continue;

                var owner = h.collider.gameObject.GetComponentInParent<NetworkInklingMovement>();

                // 죽은 플레이어의 콜라이더는 조준/총구 차단 판정에서 완전히 무시하고 통과시킨다
                if (owner != null && !owner._isAlive) continue;

                if (h.distance < nearest)
                {
                    nearest = h.distance;
                    muzzleHit = h;
                    muzzleOwner = owner;
                    muzzleBlocked = true;
                }
            }
        }

        if (muzzleBlocked)
        {
            bool isFriendly = muzzleOwner != null && muzzleOwner._teamIndex == _teamIdx;

            _hitAnything = true;
            _enemyHit = muzzleOwner != null && !isFriendly;

            if (!isFriendly)
                desiredPosition = muzzleHit.point;
        }
        else
        {
            _hitAnything = false;
            _enemyHit = false;
        }

            _targetPosition = Vector3.Lerp(_targetPosition, desiredPosition, _smoothSpeed * Time.deltaTime);
        transform.position = _targetPosition;
    }
}

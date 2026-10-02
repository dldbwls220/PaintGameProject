using UnityEngine;

public class WallClimb : MonoBehaviour
{
    [Header("Wall Detector")]
    [SerializeField] float _sphereCastRadius;
    [SerializeField] float _detectionLength;
    [SerializeField] float _yOffset;

    [Header("Climb Setting")]
    [SerializeField] float _climbSpeed = 6f;
    [SerializeField] float _climbSideSpeed = 6f;
    [SerializeField] float _idleSlideDown = 1f;
    [SerializeField] float _inputDeadzone = 0.1f;

    [Header("Angle Setting")]
    [SerializeField] float _maxWallLooking;
    [SerializeField] float _minWallSurfaceAngle = 60;
    [SerializeField] float _maxWallSurfaceAngle = 120;

    float _wallSurfaceAngle;
    RaycastHit _frontWallHit;

    public Vector3 ClimbingWall(float climbAxis, float sideAxis)  // �Ķ���ͷ� �޵��� ����
    {
        Vector3 wallRight = Vector3.Cross(Vector3.down, _frontWallHit.normal).normalized;

        Vector3 vertical = Mathf.Abs(climbAxis) > _inputDeadzone
            ? Vector3.up * climbAxis * _climbSpeed
            : Vector3.down * _idleSlideDown;   // �Է� ������ ������ ����

        Vector3 horizontal = wallRight * sideAxis * _climbSideSpeed;

        return vertical + horizontal;
    }

    // readInk가 false면(예측 재시뮬레이션 틱) 잉크 색을 GPU에서 다시 읽지 않고
    // 벽의 형태만 검사한 뒤 이전 판정 결과(wasClimbing)를 재사용한다.
    public bool CheckWall(Color teamColor, Color enemyColor, bool readInk, bool wasClimbing)
    {
        if (Physics.SphereCast(transform.position + new Vector3(0, _yOffset, 0), _sphereCastRadius, transform.forward, out _frontWallHit, _detectionLength))
        {
            // WorldInkZoneReceiver wir = _frontWallHit.collider.GetComponent<WorldInkZoneReceiver>();

            Paintabale paintable = _frontWallHit.collider.GetComponentInParent<Paintabale>();

            if (paintable != null && _frontWallHit.collider.CompareTag("Wall"))
            {
                // 탈 수 없는 각도면 잉크 색을 읽을 필요가 없다
                _wallSurfaceAngle = Vector3.Angle(_frontWallHit.normal, Vector3.up);
                if (_wallSurfaceAngle <= _minWallSurfaceAngle || _wallSurfaceAngle >= _maxWallSurfaceAngle)
                    return false;

                if (!readInk)
                    return wasClimbing;

                // SphereCast(스윕) 결과는 textureCoord가 실제 접촉점의 UV가 아니므로
                // 같은 콜라이더에 레이를 다시 쏴서 정확한 UV를 얻는다.
                RaycastHit uvHit = _frontWallHit;
                Ray uvRay = new Ray(_frontWallHit.point + _frontWallHit.normal * 0.1f, -_frontWallHit.normal);
                if (_frontWallHit.collider.Raycast(uvRay, out RaycastHit rayHit, 0.3f))
                    uvHit = rayHit;

                Color color = paintable.CheckPaintColor(uvHit);

                if (color.a < 0.1f)
                {
                     return false;
                }

                float distToMyTeam = Mathf.Abs(color.r - teamColor.r) + Mathf.Abs(color.g - teamColor.g) + Mathf.Abs(color.b - teamColor.b);

                float distToEnemyTeam = Mathf.Abs(color.r - enemyColor.r) + Mathf.Abs(color.g - enemyColor.g) + Mathf.Abs(color.b - enemyColor.b);

                return distToMyTeam < distToEnemyTeam && distToMyTeam < 0.5f;
            }
            else
                return false;
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        bool isHit = Physics.SphereCast(transform.position, _sphereCastRadius, transform.forward, out RaycastHit gizmoHit, _detectionLength);

        Gizmos.color = isHit ? Color.red : Color.blue;

        if (isHit)
        {
            Gizmos.DrawWireSphere(transform.position + new Vector3(0,_yOffset,0), _sphereCastRadius);

            Vector3 hitSphereCenter = transform.position + transform.forward * gizmoHit.distance;
            Gizmos.DrawLine(transform.position, hitSphereCenter);

            Gizmos.DrawWireSphere(hitSphereCenter, _sphereCastRadius);

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(gizmoHit.point, 0.05f);
        }
        else
        {

            Gizmos.DrawWireSphere(transform.position, _sphereCastRadius);


            Vector3 endCenter = transform.position + transform.forward * _detectionLength;
            Gizmos.DrawLine(transform.position, endCenter);


            Gizmos.DrawWireSphere(endCenter, _sphereCastRadius);
        }
    }
}

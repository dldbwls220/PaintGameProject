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

    public Vector3 ClimbingWall(float climbAxis, float sideAxis)  // 파라미터로 받도록 변경
    {
        Vector3 wallRight = Vector3.Cross(Vector3.down, _frontWallHit.normal).normalized;

        Vector3 vertical = Mathf.Abs(climbAxis) > _inputDeadzone
            ? Vector3.up * climbAxis * _climbSpeed
            : Vector3.down * _idleSlideDown;   // 입력 없으면 서서히 슬립

        Vector3 horizontal = wallRight * sideAxis * _climbSideSpeed;

        return vertical + horizontal;
    }

    public bool CheckWall(Color teamColor, Color enemyColor)
    {
        if (Physics.SphereCast(transform.position + new Vector3(0, _yOffset, 0), _sphereCastRadius, transform.forward, out _frontWallHit, _detectionLength))
        {
            WorldInkZoneReceiver wir = _frontWallHit.collider.GetComponent<WorldInkZoneReceiver>();

            if (wir != null)
            {
                Color color = wir.CheckPaintColor(_frontWallHit);

                if (color.a < 0.1f)
                {
                     return false;
                }

                float distToMyTeam = Mathf.Abs(color.r - teamColor.r) + Mathf.Abs(color.g - teamColor.g) + Mathf.Abs(color.b - teamColor.b);

                float distToEnemyTeam = Mathf.Abs(color.r - enemyColor.r) + Mathf.Abs(color.g - enemyColor.g) + Mathf.Abs(color.b - enemyColor.b);

                if (distToMyTeam < distToEnemyTeam && distToMyTeam < 0.5f)
                {
                    _wallSurfaceAngle = Vector3.Angle(_frontWallHit.normal, Vector3.up);

                    if (_wallSurfaceAngle > _minWallSurfaceAngle && _wallSurfaceAngle < _maxWallSurfaceAngle)
                    {
                        Debug.Log("벽타기 가능");
                        return true;
                    }
                }
                else if (distToEnemyTeam < distToMyTeam && distToEnemyTeam < 0.5f)
                {
                    return false;
                }           
            }
            else
                return false;
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        bool isHit = Physics.SphereCast(transform.position, _sphereCastRadius, transform.forward, out _frontWallHit, _detectionLength);

        Gizmos.color = isHit ? Color.red : Color.blue;

        if (isHit)
        {
            Gizmos.DrawWireSphere(transform.position + new Vector3(0,_yOffset,0), _sphereCastRadius);

            Vector3 hitSphereCenter = transform.position + transform.forward * _frontWallHit.distance;
            Gizmos.DrawLine(transform.position, hitSphereCenter);

            Gizmos.DrawWireSphere(hitSphereCenter, _sphereCastRadius);

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(_frontWallHit.point, 0.05f);
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

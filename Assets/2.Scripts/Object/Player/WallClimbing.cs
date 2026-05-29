using UnityEngine;
using static UnityEngine.UI.Image;

public class WallClimbing : MonoBehaviour
{

    [Header("Reference : Components")]
    [SerializeField] CharacterController _controller;
    [SerializeField] LayerMask _wallLayer;
    InklingController _inkling;
    [Space]

    [Header("Climbing Setting")]
    [SerializeField] float _climbingSpeed;
    [SerializeField] float _detectionLegth;
    [SerializeField] float _sphereCastRadius;
    [SerializeField] float _maxWallLookAngle;
    float _wallLookAnglel;

    bool _isWallFront;

    RaycastHit _frontWallHit;

    public bool _wall { get { return _isWallFront; } }

    private void Start()
    {
        _inkling = GetComponent<InklingController>();
    }

    private void Update()
    {
        WallCheck();       
    }

    public Vector3 ClimbingWall()
    {

        float my = Input.GetAxis("Vertical");

        Vector3 newvector = new Vector3(0, my, 0);

        newvector = newvector.magnitude > 1 ? newvector.normalized : newvector;

        Vector3 v = newvector * 6;

        return v;
    }

    void WallCheck()
    {
        if (Physics.SphereCast(transform.position, _sphereCastRadius, transform.forward, out _frontWallHit, _detectionLegth, _wallLayer))
        {
            Paintabale paintable = _frontWallHit.collider.GetComponent<Paintabale>();

            if (paintable != null)
            {
                Color wallColor = paintable.CheckPaintColor(_frontWallHit);

                if (wallColor.a < 0.1f)
                {
                    _isWallFront = false;
                    Debug.Log("잉크가 없는 벽입니다!");
                }

                float distToMyTeam = Mathf.Abs(wallColor.r - _inkling._myColor.r) + Mathf.Abs(wallColor.g - _inkling._myColor.g) + Mathf.Abs(wallColor.b - _inkling._myColor.b);

                float distToEnemyTeam = Mathf.Abs(wallColor.r - _inkling._otherColor.r) + Mathf.Abs(wallColor.g - _inkling._otherColor.g) + Mathf.Abs(wallColor.b - _inkling._otherColor.b);

                if (distToMyTeam < distToEnemyTeam && distToMyTeam < 0.5f)
                {
                    _isWallFront = true;
                    Debug.Log("우리 팀 벽입니다!");
                }
                else if (distToEnemyTeam < distToMyTeam && distToEnemyTeam < 0.5f)
                {
                    _isWallFront = false;
                    Debug.Log("상대 팀 벽입니다!");
                }
            }          
        }
        else _isWallFront = false;

        _wallLookAnglel = Vector3.Angle(transform.forward, -_frontWallHit.normal);

        Debug.Log("벽타기 가능?" + _isWallFront);
    }

    private void OnDrawGizmos()
    {
        bool isHit = Physics.SphereCast(transform.position, _sphereCastRadius, transform.forward, out _frontWallHit, _detectionLegth, _wallLayer);

        Gizmos.color = isHit ? Color.red : Color.blue;

        if (isHit)
        {
            Gizmos.DrawWireSphere(transform.position, _sphereCastRadius);

            Vector3 hitSphereCenter = transform.position + transform.forward * _frontWallHit.distance;
            Gizmos.DrawLine(transform.position, hitSphereCenter);

            Gizmos.DrawWireSphere(hitSphereCenter, _sphereCastRadius);

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(_frontWallHit.point, 0.05f);
        }
        else
        {
            
            Gizmos.DrawWireSphere(transform.position, _sphereCastRadius);

            
            Vector3 endCenter = transform.position + transform.forward * _detectionLegth;
            Gizmos.DrawLine(transform.position, endCenter);

            
            Gizmos.DrawWireSphere(endCenter, _sphereCastRadius);
        }
    }
}

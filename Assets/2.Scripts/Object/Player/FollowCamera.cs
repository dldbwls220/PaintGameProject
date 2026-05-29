using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] Vector3 _offset = Vector3.zero;
    [SerializeField] float _moveSpeed = 5f;
    [SerializeField] float _hDeltaRoatationScale = 2.5f;      // 좌우 회전각
    [SerializeField] float _vDelatRotationScale = 2;       // 상하 회전각
    [SerializeField] float _limitLookUpAngle = -50;     // 최대 위 보기 회전각
    [SerializeField] float _limitLookDonwAngle = 50;    // 최대 아래 보기 회전각

    Transform _followCharacter;
    Vector3 _targetPos;


    void Start()
    {
        initCam();
    }

    void Update()
    {
        Vector3 targetPosition = _followCharacter.position + _offset;

        //transform.position = Vector3.Lerp(transform.position, targetPosition, _moveSpeed * Time.deltaTime);

        transform.position = targetPosition;

        //transform.LookAt(_followCharacter);
    }

    public void initCam()
    {
        _followCharacter = GameObject.FindGameObjectWithTag("Player").transform;

        transform.position = _followCharacter.position + _followCharacter.rotation * _offset;
        transform.rotation = _followCharacter.rotation;

        _followCharacter.SendMessage("SetFollowCam", transform);
    }
}

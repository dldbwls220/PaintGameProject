using System.Collections;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class InkProjectile : MonoBehaviour
{
    [SerializeField] GameObject _testProjectile;

    InklingController _inkling;

    [SerializeField] float _minSpeed;
    [SerializeField] float _maxSpeed;
    [SerializeField] float _gravityModify;

    Vector3 _currentVelocity;
    Vector3 _currentPosition;

    private void Start()
    {
        _inkling = GetComponent<InklingController>();
    }

    public void Launch(Vector3 startPos, Vector3 dir)
    {
        GameObject go = Instantiate(_testProjectile, startPos, Quaternion.identity);

        StartCoroutine(SimulateArc(startPos, dir, go));
    }

    IEnumerator SimulateArc(Vector3 startPos, Vector3 direction , GameObject projectile)
    {
        float ranSpeed = Random.Range(_minSpeed, _maxSpeed);

        Vector3 currentPos = startPos;
        Vector3 currentVelocity = direction.normalized * ranSpeed;

        float timeStep = Time.deltaTime;

        // 무한히 떨어지는 것을 방지하기 위한 루프
        while (currentPos.y > -20f) 
        {
            //중력 적용
            currentVelocity.y += _gravityModify * Physics.gravity.y * timeStep;

            //도달할 예상 위치 계산
            Vector3 nextPos = currentPos + (currentVelocity * timeStep);

            //현제 위치에서 다음 위치의 방향과 거리 계산
            Vector3 displacement = nextPos - currentPos;
            float distance = displacement.magnitude;
            Vector3 dir = displacement.normalized;

            if (projectile != null)
            {
                projectile.transform.position = currentPos;

                projectile.transform.forward = dir;
            }

            //레이케스트 발사
            if (Physics.Raycast(currentPos, dir, out RaycastHit hit, distance))
            {
                if (projectile != null)
                {
                    // 1. 잉크 방울이 팡! 하고 터지는 이펙트나 사운드 연출 생성
                    // PlaySplashEffect(hit.point);

                    // 2. 잉크 오브젝트를 메모리에서 깔끔하게 삭제 (사라지게 함)
                    Destroy(projectile);
                }

                // 잉크 칠하는 로직 실행
                PaintInk(hit);
                Debug.DrawLine(currentPos, nextPos, Color.cyan, 1f);
                yield break;
            }

            //충돌이 없으면 위치 갱신
            currentPos = nextPos;

            Debug.DrawLine(currentPos, nextPos, Color.cyan, 1f);

            yield return null;
        }

        if (projectile != null) Destroy(projectile);
    }

    void PaintInk(RaycastHit hit)
    {
        // hit.point = 부딪힌 3D 좌표
        // hit.textureCoord = 부딪힌 메쉬의 UV 좌표 (여기에 잉크 텍스처를 그림)

        Paintabale p = hit.transform.GetComponent<Paintabale>();
        
        if (p != null)
        {
            float radius = Random.Range(0.9f, 1.7f);
            PaintManager.instance.paint(p, hit.point, radius, 1, 1, _inkling._myColor);
        }

        Debug.Log($"잉크 충돌! 위치: {hit.point}");

        // 서버에는 파티클 수천 개를 보낼 필요 없이, 
        // "hit.point(또는 UV)"랑 "잉크 반경" 딱 두 개만 패킷으로 보내면 동기화 끝입니다.
    }
}

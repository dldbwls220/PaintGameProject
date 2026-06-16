using System.Collections;
using UnityEngine;
using DefineEnum;
using Unity.VisualScripting;

public class InkProjectile : MonoBehaviour
{
    [SerializeField] GameObject _testProjectile;
    [SerializeField] ParticleSystem _shootFX;

    [SerializeField] InklingController _inkling; // test

    [Header("Ink Projectile Setting")]
    [SerializeField] float _minSpeed;
    [SerializeField] float _maxSpeed;
    [SerializeField] float _gravityModify;

    [Header("Ink Paint Setting")]
    [SerializeField] float _minRadius = 0.5f;
    [SerializeField] float _maxRadius = 1.2f;
    [SerializeField] float _strength = 1;
    [SerializeField] float _hardness = 1;

    private void Start()
    {
        //_inkling = GetComponent<InklingController>();
        _testProjectile = Resources.Load<GameObject>("TestProjectile");
    }

    public void Launch(Vector3 startPos, Vector3 dir)
    {
        _shootFX.Play();
        //ObjectPool._instance.Spawn();
        StartCoroutine(SimulateArc(startPos, dir));
    }

    IEnumerator SimulateArc(Vector3 startPos, Vector3 direction)
    {
        //GameObject projectile = Instantiate(_testProjectile, startPos, Quaternion.identity);

        float ranSpeed = Random.Range(_minSpeed, _maxSpeed);

        Vector3 currentPos = startPos;
        Vector3 currentVelocity = direction.normalized * ranSpeed;

        float timeStep = Time.deltaTime;

        GameObject projectile = GameManager._instance._pool.Get(InkProjectileState.InkBullet);

        while (currentPos.y > -20f) 
        {
            currentVelocity.y += _gravityModify * Physics.gravity.y * timeStep;

            Vector3 nextPos = currentPos + (currentVelocity * timeStep);

            Vector3 displacement = nextPos - currentPos;
            float distance = displacement.magnitude;
            Vector3 dir = displacement.normalized;        

            if (projectile != null)
            {
                projectile.transform.position = currentPos;

                projectile.transform.forward = dir;
            }

            if (Physics.Raycast(currentPos, dir, out RaycastHit hit, distance))
            {
                if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Wall") || hit.transform.gameObject.layer == LayerMask.NameToLayer("Ground"))
                {
                    if (projectile != null)
                    {
                        GameObject splashP = GameManager._instance._pool.Get(InkProjectileState.InkSplash);
                        splashP.transform.position = currentPos;

                        if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Wall"))
                        {
                            splashP.transform.rotation = Quaternion.Euler(-90, 0, 0);
                        }
                        else if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Ground"))
                        {
                            splashP.transform.rotation = Quaternion.Euler(0, 0, 0);
                        }

                        
                        projectile.SetActive(false);
                    }

                   
                    PaintInk(hit);
                    Debug.DrawLine(currentPos, nextPos, Color.cyan, 1f);
                }
                else if(hit.transform.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                {
                    if (projectile != null)
                    {
                        SoundManager._instance.PlaySFX(SFXName.Hit_Inkling_00);

                        GameObject hitP = GameManager._instance._pool.Get(InkProjectileState.InkHit);
                        hitP.transform.position = currentPos;

                       
                        projectile.SetActive(false);
                    }
                    Debug.DrawLine(currentPos, nextPos, Color.red, 1f);
                }
               

                yield break;
            }

           
            currentPos = nextPos;

            Debug.DrawLine(currentPos, nextPos, Color.cyan, 1f);

            yield return null;
        }

        if (projectile != null) projectile.SetActive(false); 
    }

    void PaintInk(RaycastHit hit)
    {
        float radius1 = Random.Range(_minRadius, _maxRadius);

        WorldInkManager.instance.Paint(hit.point, Color.aquamarine, radius1, _hardness);

        Paintabale p = hit.transform.GetComponentInParent<Paintabale>();

        if (p != null)
        {
            float radius = Random.Range(_minRadius, _maxRadius);
            PaintManager.instance.paint(p, hit.point, radius, _hardness, _strength, /*_inkling._myColor*/ Color.aquamarine);
            Debug.Log($"잉크 충돌! 위치: {hit.point}");
        }
        else
        {
            Debug.LogWarning($"Paintabale 컴포넌트를 찾을 수 없음: {hit.transform.name} (부모 포함)");
        }
    }

}

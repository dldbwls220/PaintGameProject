using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DefineEnum;

[System.Serializable]
public class ObjectInfo
{
    public string _name;
    public GameObject _objPrefab;
    public int _count;
    public Transform _tfPoolParent;
}

public class PoolManager : MonoBehaviour
{
    static PoolManager _uniqueInstance;

    [SerializeField] ObjectInfo[] _objInfo;

    [SerializeField] List<GameObject>[] _poolList;   

    public static PoolManager _instance { get { return _uniqueInstance; } }

    // 프리팹 자체를 키로 쓰는 FX 풀. Inspector 등록 없이 처음 요청될 때 해당 프리팹의 풀이 생긴다.
    readonly Dictionary<GameObject, Queue<GameObject>> _fxPool = new();

    void Awake()
    {
        _uniqueInstance = this;
    }

    private void Start()
    {
        InitPool();
    }

    public void InitPool()
    {
        _poolList = new List<GameObject>[_objInfo.Length];

        for (int i = 0; i < _poolList.Length; i++)
        {
            _poolList[i] = new List<GameObject>();
        }
       
    }

    public GameObject Get(InkProjectileState state)
    {
        GameObject select = null;

        ObjectInfo info = _objInfo[(int)state];

        foreach (GameObject obj in _poolList[(int)state])
        {
            if (!obj.activeSelf)
            {
                select = obj;

                if (select.TryGetComponent<TrailRenderer>(out var trail))
                {
                    trail.Clear();
                }

                select.SetActive(true);

                Debug.Log("프리팹 제사용");

                break;
            }
        }
      
        if (select == null)
        {
            select = Instantiate(info._objPrefab, info._tfPoolParent);
            _poolList[(int)state].Add(select);

            Debug.Log("프리팹 생성");
        }

        Debug.Log(select);

        return select;
    }

    // 풀에서 FX를 꺼내 지정 위치에 켜고, lifeTime 후 자동으로 꺼서 풀에 돌려놓는다
    public GameObject GetFX(GameObject prefab, Vector3 position, Quaternion rotation, float lifeTime)
    {
        if (!_fxPool.TryGetValue(prefab, out var queue))
            _fxPool[prefab] = queue = new Queue<GameObject>();

        GameObject fx = null;
        while (fx == null && queue.Count > 0)
            fx = queue.Dequeue(); // 외부에서 파괴된 인스턴스는 건너뛴다

        if (fx == null)
        {
            // 위치를 지정해 생성해야 Play On Awake로 첫 프레임에 원점에서 터지지 않는다
            fx = Instantiate(prefab, position, rotation, transform);
        }
        else
        {
            // 꺼진 상태에서 위치를 먼저 옮긴 뒤 켜야 OnEnable 재생이 올바른 위치에서 시작된다
            fx.transform.SetPositionAndRotation(position, rotation);
            fx.SetActive(true);
        }

        StartCoroutine(ReleaseFXAfter(fx, queue, lifeTime));
        return fx;
    }

    IEnumerator ReleaseFXAfter(GameObject fx, Queue<GameObject> queue, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (fx == null) yield break;

        fx.SetActive(false);
        queue.Enqueue(fx);
    }
}

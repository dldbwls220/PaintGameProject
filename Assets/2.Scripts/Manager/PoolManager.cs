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

                Debug.Log("橇府普 力荤侩");

                break;
            }
        }
      
        if (select == null)
        {
            select = Instantiate(info._objPrefab, info._tfPoolParent);
            _poolList[(int)state].Add(select);

            Debug.Log("橇府普 积己");
        }

        Debug.Log(select);

        return select;
    }
}

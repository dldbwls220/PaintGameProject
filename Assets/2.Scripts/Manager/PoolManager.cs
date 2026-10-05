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

    // 인덱스는 FXState 순서와 일치해야 한다 (Splash, HitParticle, Morph, Swim)
    [SerializeField] ObjectInfo[] _objInfo;

    // 비활성 상태로 대기 중인 인스턴스만 들어 있다. 사용 중인 FX는 ReleaseFXAfter에서 반납된다.
    Stack<GameObject>[] _poolStack;

    public static PoolManager _instance => _uniqueInstance;

    void Awake()
    {
        _uniqueInstance = this;

        // 다른 오브젝트의 Start에서 GetFX를 호출해도 안전하도록 Awake에서 초기화한다
        InitPool();
    }

    public void InitPool()
    {
        _poolStack = new Stack<GameObject>[_objInfo.Length];

        for (int i = 0; i < _poolStack.Length; i++)
        {
            _poolStack[i] = new Stack<GameObject>(_objInfo[i]._count);

            // _count만큼 미리 만들어 두어 게임 중 첫 생성 스파이크를 줄인다
            for (int j = 0; j < _objInfo[i]._count; j++)
            {
                GameObject obj = Instantiate(_objInfo[i]._objPrefab, _objInfo[i]._tfPoolParent);
                obj.SetActive(false);
                _poolStack[i].Push(obj);
            }
        }
    }

    public GameObject GetFX(FXState state, Vector3 position, Quaternion rotation, float lifeTime)
    {
        ObjectInfo info = _objInfo[(int)state];
        Stack<GameObject> stack = _poolStack[(int)state];

        GameObject fx = null;
        while (fx == null && stack.Count > 0)
            fx = stack.Pop(); 

        if (fx == null)
        {

            fx = Instantiate(info._objPrefab, position, rotation, info._tfPoolParent);
        }
        else
        {
            fx.transform.SetPositionAndRotation(position, rotation);
            fx.SetActive(true);
        }

        StartCoroutine(ReleaseFXAfter(fx, stack, lifeTime));
        return fx;
    }

    IEnumerator ReleaseFXAfter(GameObject fx, Stack<GameObject> stack, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (fx == null) yield break;

        fx.SetActive(false);
        stack.Push(fx);
    }
}

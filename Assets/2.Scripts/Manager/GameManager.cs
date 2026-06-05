using UnityEngine;

public class GameManager : MonoBehaviour
{
    static GameManager _uniqueinstance;

    public PoolManager _pool;

    public static GameManager _instance { get { return _uniqueinstance; } }

    void Awake()
    {
        _uniqueinstance = this;
    }

    
}

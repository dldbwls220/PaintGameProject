using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class PooledNetworkObjectProvider : NetworkObjectProviderDefault
{
    [SerializeField] List<NetworkObject> _pooledPrefabs;

    readonly Dictionary<NetworkObject, Stack<NetworkObject>> _pool = new();

    void Awake()
    {
        foreach (var prefab in _pooledPrefabs)
            _pool[prefab] = new Stack<NetworkObject>();
    }

    protected override NetworkObject InstantiatePrefab(NetworkRunner runner, NetworkObject prefab)
    {
        if (_pool.TryGetValue(prefab, out var stack) && stack.Count > 0)
        {
            var instance = stack.Pop();
            instance.transform.SetParent(null, false); // 루트여야 이후 씬 이동이 동작함
            instance.gameObject.SetActive(true);
            return instance;
        }
        return base.InstantiatePrefab(runner, prefab);
    }

    protected override void DestroyPrefabInstance(NetworkRunner runner, NetworkPrefabId prefabId, NetworkObject instance)
    {
        var prefab = runner.Prefabs.Load(prefabId, isSynchronous: true);
        if (prefab == null || !_pool.TryGetValue(prefab, out var stack))
        {
            base.DestroyPrefabInstance(runner, prefabId, instance);
            return;
        }

        instance.gameObject.SetActive(false);
        instance.transform.SetParent(transform, false); // Runner(DontDestroyOnLoad) 아래에 보관
        stack.Push(instance);
    }
}

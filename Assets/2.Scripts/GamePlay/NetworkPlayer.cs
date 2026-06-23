using UnityEngine;
using Fusion;
using UnityEngine.SocialPlatforms;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{
    static NetworkPlayer _uniqueinstance;

    public static NetworkPlayer _instance { get { return _uniqueinstance; } }

    [Networked] public int SpawnIndex { get; set; }

    public void SetSpawnIndex(int index)
    {
        SpawnIndex = index;
        gameObject.name = $"Player {index}";
    }

    public override void Spawned()
    {
        gameObject.name = $"Player {SpawnIndex}";

        if (Object.HasInputAuthority)
        {
            _uniqueinstance = this;
            Debug.Log($"Spawned local Player {SpawnIndex}");
        }
        else Debug.Log($"Spawned remote Player {SpawnIndex}");
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (player == Object.InputAuthority)
            Runner.Despawn(Object);
    }
}

using UnityEngine;
using Fusion;
using UnityEngine.SocialPlatforms;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{
    static NetworkPlayer _uniqueinstance;

    public static NetworkPlayer _instance { get { return _uniqueinstance; } }

    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            _uniqueinstance = this;

            Debug.Log("Spawned local Player");
        }
        else Debug.Log("Spawned remote Player");
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (player == Object.InputAuthority)
            Runner.Despawn(Object);
    }
}

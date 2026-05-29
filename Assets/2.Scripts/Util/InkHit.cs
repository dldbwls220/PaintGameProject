using UnityEngine;

public class InkHit : MonoBehaviour
{
    CharBase _owner;

    public void InitAttackOwner(CharBase owner)
    {
        _owner = owner;
    }

    public T GetOwner<T>() where T : CharBase
    {
        return(T) _owner;
    }

    
}

using UnityEngine;

public class TestEnemy : MonoBehaviour
{
    [SerializeField] float _hp;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("AttackInk"))
        {
            Debug.Log("피격 확인");
            Destroy(other.gameObject);
        }
    }
}

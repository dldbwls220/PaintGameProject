using UnityEngine;

public class CharacterSeperation : MonoBehaviour
{
    [SerializeField] LayerMask _targetLayer;
    [SerializeField] float _radius = 1f;
    [SerializeField] float _pushStrength = 5f;
    [SerializeField] float _maxPushSpeed = 4f;

    public Vector3 GetPushVelocity(NetworkInklingMovement self)
    {
        Vector3 selfPosition = self.transform.position;
        Vector3 push = Vector3.zero;

        Collider[] colliders = Physics.OverlapSphere(selfPosition, _radius, _targetLayer);

        foreach (Collider hit in colliders)
        {
            NetworkInklingMovement other = hit.GetComponentInParent<NetworkInklingMovement>();
            if (other == null || other == self) continue;

            Vector3 offset = selfPosition - other.transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;

            Vector3 direction;
            if (distance > 0.0001f)
            {
                direction = offset / distance;
            }
            else
            {
                // 완전히 겹친 경우, 양쪽 클라이언트가 항상 같은 결과를 내도록 NetworkId로 방향을 결정
                direction = self.Object.Id.Raw < other.Object.Id.Raw ? Vector3.right : Vector3.left;
            }

            float overlap = _radius - distance;
            if (overlap <= 0f) continue;

            push += direction * overlap * _pushStrength;
        }

        return Vector3.ClampMagnitude(push, _maxPushSpeed);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}

using UnityEngine;

public class MouseTarget : MonoBehaviour
{
    [SerializeField] LayerMask _playerLayer;

    void Update()
    {
        GetMousePos();
    }

    void GetMousePos()
    {
        Ray ray = Camera.main.ScreenPointToRay( Input.mousePosition );

        // playerLayer¸¸ Á¦¿Ü
        int mask = ~_playerLayer;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, mask))
        {
            if(hit.collider)
            transform.position = hit.point;
        }

    }
}

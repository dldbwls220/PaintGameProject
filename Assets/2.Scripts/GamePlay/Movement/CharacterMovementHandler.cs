using Fusion;
using UnityEngine;

public class CharacterMovementHandler : NetworkBehaviour
{
    NetworkInklingController _inklingController;

    void Awake()
    {
        _inklingController = GetComponent<NetworkInklingController>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData networkInputData))
        {
            Vector3 moveDirection = transform.forward * networkInputData._movementInput.y + transform.right * networkInputData._movementInput.x;
            moveDirection.Normalize();

            _inklingController.Move(moveDirection);
        }
    }
}

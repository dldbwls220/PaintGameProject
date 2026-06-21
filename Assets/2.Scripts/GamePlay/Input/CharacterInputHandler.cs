using UnityEngine;

public class CharacterInputHandler : MonoBehaviour
{
    Vector3 _moveInputVector = Vector3.zero;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //Move Input
        _moveInputVector.x = Input.GetAxis("Horizontal");
        _moveInputVector.z = Input.GetAxis("Vertical");
    }

    public NetworkInputData GetNetworkInput()
    {
        NetworkInputData inputdata = new NetworkInputData();

        inputdata._movementInput = _moveInputVector;

        return inputdata;
    }
}

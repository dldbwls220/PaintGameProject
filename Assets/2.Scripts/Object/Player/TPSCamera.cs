using UnityEngine;

public class TPSCamera : MonoBehaviour
{
    Transform _followCharacter;

    private void Start()
    {
        //initCam();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    //public void initCam()
    //{
    //    _followCharacter = GameObject.FindGameObjectWithTag("Player").transform;

    //    _followCharacter.SendMessage("SetFollowCam", transform);
    //}
}

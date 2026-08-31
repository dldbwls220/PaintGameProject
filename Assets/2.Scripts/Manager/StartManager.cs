using UnityEngine;

public class StartManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (GameSoundManager.instance != null)
        {
            GameSoundManager.instance.LoadAllSound();
            ResourcePoolManager.instance.AllLoadResources();
            PlayerCustomizeManager.instance.initDefaultcustom();
        }

        WipeTransitionManager.instance.StartScene();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

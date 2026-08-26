using UnityEngine;

public class EmoteCharacter : MonoBehaviour
{
    [Header("Eyelids Setting")]
    [SerializeField] SkinnedMeshRenderer _eyelidsSMR;

    MaterialPropertyBlock _mpb;

    private void Start()
    {
        _mpb = new MaterialPropertyBlock();
    }

    public void Update()
    {

    }

    public void InitCharacter()
    {
        
    }

    public void CloseEye()
    {
      
        _eyelidsSMR.GetPropertyBlock(_mpb);
        _mpb.SetColor("_BaseColor", new Color(1,1,1,1));
        _eyelidsSMR.SetPropertyBlock(_mpb);

       
    }

    public void CompletelyCloseEye()
    {
        _eyelidsSMR.GetPropertyBlock(_mpb);
        _mpb.SetColor("_BaseColor", new Color(0, 0, 0, 1));
        _eyelidsSMR.SetPropertyBlock(_mpb);
    }

    public void OpenEye()
    {
        _eyelidsSMR.GetPropertyBlock(_mpb);
        _mpb.SetColor("_BaseColor", new Color(1, 1, 1, 0));
        _eyelidsSMR.SetPropertyBlock(_mpb);
    }
}

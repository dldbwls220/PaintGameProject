using DefineEnum;
using UnityEngine;

public class JudgeFlagObject : MonoBehaviour
{
    [SerializeField] SkinnedMeshRenderer _flagMesh;
    [SerializeField] Animator _flagAnim;
    MaterialPropertyBlock _mpb;

    public void InitFlag(Color color)
    {
        _mpb = new MaterialPropertyBlock();
     
        _flagMesh.GetPropertyBlock(_mpb);
        _mpb.SetColor("_TintColor", color);
        _flagMesh.SetPropertyBlock(_mpb);
        _flagMesh.enabled = false;
    }

    public void StartAnim()
    {
        _flagAnim.SetBool("IsJudging", /*GameManager._instance._isJudging*/ true);
    }

    public void ActiveFlag()
    {
        _flagMesh.enabled = true;
    }
}

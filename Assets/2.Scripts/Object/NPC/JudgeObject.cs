using UnityEngine;
using DefineEnum;
using System.Linq;

public class JudgeObject : MonoBehaviour
{
    [Header("Anim & Texture Setting")]
    [SerializeField] Animator _judgeAnimator;
    [SerializeField] Texture[] _faceTexture;
    [SerializeField] Texture[] _faceNormal;
    [SerializeField] Texture[] _faceRoughness;
    [SerializeField] SkinnedMeshRenderer _faceMesh;
    [Header("ReferenceClass")]
    [SerializeField] JudgeFlagObject _flagObject;
    MaterialPropertyBlock _mpb;

    [Header("ColorDebug")]
    [SerializeField] Color _DeBugcolor;

    private void Start()
    {
        //SetJudgeFace(JudgeNJudgeJrFaceState.Default);

        InitJudge();
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.Q))
        {
            JudgeAnim(ResultState.Win, true);
        }
    }

    public void InitJudge()
    {
        _flagObject.InitFlag(_DeBugcolor);
        _mpb = new MaterialPropertyBlock();
    }

    public void JudgeAnim(ResultState state, bool _isJudging)
    {
        _flagObject.StartAnim();
        _judgeAnimator.SetInteger("AniState", (int)state);
        _judgeAnimator.SetBool("IsJudging", _isJudging);
    }

    public void SetJudgeFace(JudgeNJudgeJrFaceState state)
    {
        if (_faceTexture.Length == 0) return;

        _faceMesh.GetPropertyBlock(_mpb);
        _mpb.SetTexture("_BaseMap", _faceTexture[(int)state]);
        _mpb.SetTexture("_NormalMap", _faceNormal[(int)state]);
        _mpb.SetTexture("_MetallicMap", _faceRoughness[(int)state]);
        _faceMesh.SetPropertyBlock(_mpb);

        //_faceMesh.material.SetTexture("_BaseMap", _faceTexture[(int)state]);
        //_faceMesh.material.SetTexture("_NormalMap", _faceNormal[(int)state]);
        //_faceMesh.material.SetTexture("_MetallicMap", _faceRoughness[(int)state]);
    }

    public void OpenFlag()
    {
        _flagObject.ActiveFlag();
    }
}

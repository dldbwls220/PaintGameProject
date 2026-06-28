using UnityEngine;
using DefineEnum;
using UnityEngine.Animations.Rigging;

public class InklingRenderController : MonoBehaviour
{
    [Header("Model Object")]
    [SerializeField] GameObject[] _modelObj;

    [Header("Animation")]
    [SerializeField] Animator[] _animator;
    [SerializeField] MultiAimConstraint _multiAC;

    [Header("Camer Transparency")]
    [SerializeField] float _maxAlpha = 1;
    [SerializeField] float _minAlpha = 0.2f;
    [SerializeField] float _upperThreshold = 35f;
    [SerializeField] float _lowerThreshold = -35f;

    SkinnedMeshRenderer[] _inklingRender;
    SkinnedMeshRenderer[] _halfRender;
    SkinnedMeshRenderer[] _squidRender;
    MaterialPropertyBlock _mpb;

    // NetworkInklingMovement가 매 프레임 채워서 넘기는 스냅샷
    public struct RenderState
    {
        public bool isSquid;
        public bool isMorphingSquid;
        public bool isMorphingInkling;
        public bool isGrounded;
        public bool isShooting;
        public bool isMoving;
        public bool switchFoot;
        public float layerWeight;
        public Vector3 localMoveVelocity;   // InverseTransformVector 결과         
        public float cameraAngleX;           // HasInputAuthority일 때만 유효
        public bool hasInputAuthority;
    }

    public void Init()
    {
        _inklingRender = _modelObj[(int)FormState.Inkling].GetComponentsInChildren<SkinnedMeshRenderer>();
        _halfRender = _modelObj[(int)FormState.Half].GetComponentsInChildren<SkinnedMeshRenderer>();
        _squidRender = _modelObj[(int)FormState.Squid].GetComponentsInChildren<SkinnedMeshRenderer>();
        _mpb = new MaterialPropertyBlock();

        SwitchRender(FormState.Inkling, true);
        SwitchRender(FormState.Half, false);
        SwitchRender(FormState.Squid, false);
    }

    // NetworkInklingMovement.Render() 에서 호출
    public void UpdateRender(in RenderState s)
    {
        UpdateFormRender(s);
        UpdateAnimation(s);

        if (s.hasInputAuthority)
            ApplyCameraTransparency(s.cameraAngleX);
    }

    void UpdateFormRender(in RenderState s)
    {

        if (s.isMorphingSquid)
        {
            SwitchRender(FormState.Inkling, false);
            SwitchRender(FormState.Half, true);
            SwitchRender(FormState.Squid, false);
        }
        else if (s.isSquid)
        {
            SwitchRender(FormState.Inkling, false);
            SwitchRender(FormState.Half, false);
            SwitchRender(FormState.Squid, true);
        }
        else if (s.isMorphingInkling)
        {
            SwitchRender(FormState.Inkling, false);
            SwitchRender(FormState.Half, true);
            SwitchRender(FormState.Squid, false);
        }
        else
        {
            SwitchRender(FormState.Inkling, true);
            SwitchRender(FormState.Half, false);
            SwitchRender(FormState.Squid, false);
        }
    }

    void UpdateAnimation(in RenderState s)
    {
        var inkAnim = _animator[(int)FormState.Inkling];
        inkAnim.SetBool("isSquid", s.isSquid);
        inkAnim.SetBool("isShooting", s.isShooting);
        inkAnim.SetBool("isGround", s.isGrounded);
        inkAnim.SetBool("isJumping", !s.isGrounded);
        inkAnim.SetBool("FootSwitch", s.switchFoot);
        inkAnim.SetLayerWeight(1, s.layerWeight);
        _multiAC.weight = s.layerWeight;

        var halfAnim = _animator[(int)FormState.Half];
        halfAnim.SetBool("isSquid", s.isSquid);

        var squidAnim = _animator[((int)FormState.Squid)];
        squidAnim.SetBool("isGround", s.isGrounded);
        squidAnim.SetBool("isJumping", !s.isGrounded);

        if (s.isMorphingSquid)
        {
            inkAnim.SetInteger("AniState", (int)AniState.Morph_toSquid);
            halfAnim.SetInteger("AniState", (int)AniState.Morph_toSquid);
        }
        else if (s.isMorphingInkling)
        {
            inkAnim.SetTrigger("ToHuman");
            halfAnim.SetTrigger("ToHuman");
            inkAnim.SetInteger("AniState", (int)AniState.Morph_toHuman);
            halfAnim.SetInteger("AniState", (int)AniState.Morph_toHuman);
        }
        else if (s.isGrounded)
        {
            if (s.isSquid)
            {
               squidAnim.SetInteger("AniState", s.isMoving ? (int)AniState.Squid_Walk : (int)AniState.Squid_Idle);
            }
            else
            {
                inkAnim.SetInteger("AniState", s.isMoving ? (int)AniState.Run : (int)AniState.Idle);
                halfAnim.SetInteger("AniState", s.isMoving ? (int)AniState.Run : (int)AniState.Idle);

                inkAnim.SetFloat("RNL", s.localMoveVelocity.x);
                inkAnim.SetFloat("FNB", s.localMoveVelocity.z);

                halfAnim.SetFloat("RNL", s.localMoveVelocity.x);
                halfAnim.SetFloat("FNB", s.localMoveVelocity.z);
            }
        }
        else
        {
            inkAnim.SetInteger("AniState", (int)AniState.Jump);
        }
    }

    void ApplyCameraTransparency(float angle)
    {
        float alpha = 1;
        float dither = 0;

        if (angle > _upperThreshold)
        {          
            float t = (angle - _upperThreshold) / (70f - _upperThreshold);
            alpha = Mathf.Lerp(_maxAlpha, _minAlpha, t);
            dither = Mathf.Lerp(_minAlpha, _maxAlpha, t);
        }
        else if (angle < _lowerThreshold)
        {
            float t = (_lowerThreshold - angle) / Mathf.Abs(-70f - _lowerThreshold);
            alpha = Mathf.Lerp(_maxAlpha, _minAlpha, t);
            dither = Mathf.Lerp(_minAlpha, _maxAlpha, t);
        }

        foreach (var ren in _inklingRender)
        {
            ren.GetPropertyBlock(_mpb);
            _mpb.SetColor("_BaseColor", new Color(1, 1, 1, alpha));
            ren.SetPropertyBlock(_mpb);
        }
    }

    void SwitchRender(FormState state, bool isOn)
    {
        var targets = state switch
        {
            FormState.Inkling    => _inklingRender,
            FormState.Half       => _halfRender,
            FormState.Squid      => _squidRender,
            _                    => null
        };

        if (targets == null) return;
        foreach(var r in targets) r.enabled = isOn;
    }
}

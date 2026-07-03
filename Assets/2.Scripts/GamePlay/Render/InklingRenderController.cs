using UnityEngine;
using DefineEnum;
using UnityEngine.Animations.Rigging;

public class InklingRenderController : MonoBehaviour
{
    [Header("Model Object")]
    [SerializeField] GameObject[] _modelObj;
    [SerializeField] GameObject _inkTankObj;

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
    MeshRenderer[] _inkTankRender;
    MaterialPropertyBlock _mpb;

    public struct RenderState
    {
        public bool isSquid;
        public bool isMorphingSquid;
        public bool isMorphingInkling;
        public bool isGrounded;
        public bool isShooting;
        public bool isMoving;
        public bool switchFoot;
        public bool isSameColor;
        public bool isSwimming;
        public bool isSlowed;
        public float layerWeight;
        public Vector3 localMoveVelocity;
        public Color teamColor;
        public float cameraAngleX;
        public float inktankOffset;
        public bool hasInputAuthority;
    }

    public void Init()
    {
        _inklingRender = _modelObj[(int)FormState.Inkling].GetComponentsInChildren<SkinnedMeshRenderer>();
        _halfRender = _modelObj[(int)FormState.Half].GetComponentsInChildren<SkinnedMeshRenderer>();
        _squidRender = _modelObj[(int)FormState.Squid].GetComponentsInChildren<SkinnedMeshRenderer>();
        _inkTankRender = _inkTankObj.GetComponentsInChildren<MeshRenderer>();
        _mpb = new MaterialPropertyBlock();

        SwitchRender(FormState.Inkling, true);
        SwitchRender(FormState.Half, false);
        SwitchRender(FormState.Squid, false);
        InkTankRender(false);
    }

    public void SetTeamColor(Color teamColor)
    {
        foreach (SkinnedMeshRenderer ren in _inklingRender)
        {
            if (ren.name.Contains("_TeamC"))
            {
                ren.GetPropertyBlock(_mpb);
                _mpb.SetColor("_BaseColor", teamColor);
                ren.SetPropertyBlock(_mpb);
            }

            if (ren.name.Contains("_TeamE"))
            {
                ren.GetPropertyBlock(_mpb);
                _mpb.SetColor("_EmissionColor", teamColor);
                ren.SetPropertyBlock(_mpb);
            }
        }

        foreach (SkinnedMeshRenderer ren in _halfRender)
        {
            if (ren.name.Contains("_TeamC"))
            {
                ren.GetPropertyBlock(_mpb);
                _mpb.SetColor("_BaseColor", teamColor);
                ren.SetPropertyBlock(_mpb);
            }
            else if (ren.name.Contains("_TeamE"))
            {
                ren.GetPropertyBlock(_mpb);
                _mpb.SetColor("_EmissionColor", teamColor);
                ren.SetPropertyBlock(_mpb);
            }
        }

        foreach (SkinnedMeshRenderer ren in _squidRender)
        {
            if (ren.name.Contains("_TeamC"))
            {
                ren.GetPropertyBlock(_mpb);
                _mpb.SetColor("_BaseColor", teamColor);
                _mpb.SetColor("_EmissionColor", teamColor);
                ren.SetPropertyBlock(_mpb);
            }
        }

        foreach (MeshRenderer ren in _inkTankRender)
        {
            if (ren.name.Contains("M_Ink"))
            {
                ren.GetPropertyBlock(_mpb);
                _mpb.SetColor("_BaseColor", teamColor);
                ren.SetPropertyBlock(_mpb);
            }
        }
    }

    // NetworkInklingMovement.Render() ���� ȣ��
    public void UpdateRender(in RenderState s)
    {
        UpdateFormRender(s);
        UpdateAnimation(s);
        UpdateInkRefillRender(s);

        if (s.hasInputAuthority)
            ApplyCameraTransparency(s);
    }

    void UpdateFormRender(in RenderState s)
    {

        if (s.isMorphingSquid)
        {
            SwitchRender(FormState.Inkling, false);
            SwitchRender(FormState.Half, true);
            SwitchRender(FormState.Squid, false);
            InkTankRender(false);
        }
        else if (s.isSquid)
        {
            if(s.isSwimming)
            {
                if (!s.isGrounded)
                {
                    SwitchRender(FormState.Inkling, false);
                    SwitchRender(FormState.Half, false);
                    SwitchRender(FormState.Squid, true);
                    InkTankRender(false);
                }
                else
                {
                    SwitchRender(FormState.Inkling, false);
                    SwitchRender(FormState.Half, false);
                    SwitchRender(FormState.Squid, false);
                    InkTankRender(false);
                }             
            }
            else
            {
                SwitchRender(FormState.Inkling, false);
                SwitchRender(FormState.Half, false);
                SwitchRender(FormState.Squid, true);
                InkTankRender(false);
            }
           
        }
        else if (s.isMorphingInkling)
        {
            SwitchRender(FormState.Inkling, false);
            SwitchRender(FormState.Half, true);
            SwitchRender(FormState.Squid, false);
            InkTankRender(false);
        }
        else
        {
            SwitchRender(FormState.Inkling, true);
            SwitchRender(FormState.Half, false);
            SwitchRender(FormState.Squid, false);
            InkTankRender(true);
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
        inkAnim.SetBool("isSlowed", s.isSlowed);
        inkAnim.SetLayerWeight(1, s.layerWeight);
        _multiAC.weight = s.layerWeight;

        var halfAnim = _animator[(int)FormState.Half];
        halfAnim.SetBool("isSquid", s.isSquid);
        halfAnim.SetBool("isSlowed", s.isSlowed);

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
                if (s.isSlowed)
                {
                    inkAnim.SetInteger("AniState", s.isMoving ? (int)AniState.Slowed_Walk : (int)AniState.Slowed);
                    halfAnim.SetInteger("AniState", s.isMoving ? (int)AniState.Slowed_Walk : (int)AniState.Slowed);
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
        }
        else
        {
            inkAnim.SetInteger("AniState", (int)AniState.Jump);
        }
    }

    void ApplyCameraTransparency(in RenderState s)
    {
        float alpha = 1;
        float dither = 0;

        if (s.cameraAngleX > _upperThreshold)
        {          
            float t = (s.cameraAngleX - _upperThreshold) / (70f - _upperThreshold);
            alpha = Mathf.Lerp(_maxAlpha, _minAlpha, t);
            dither = Mathf.Lerp(_minAlpha, _maxAlpha, t);
        }
        else if (s.cameraAngleX < _lowerThreshold)
        {
            float t = (_lowerThreshold - s.cameraAngleX) / Mathf.Abs(-70f - _lowerThreshold);
            alpha = Mathf.Lerp(_maxAlpha, _minAlpha, t);
            dither = Mathf.Lerp(_minAlpha, _maxAlpha, t);
        }

        foreach (var ren in _inklingRender)
        {
            ren.GetPropertyBlock(_mpb);
            Color baseColor = ren.name.Contains("_TeamC")
                ? s.teamColor
                : Color.white;
            _mpb.SetColor("_BaseColor", new Color(baseColor.r, baseColor.g, baseColor.b, alpha));
            ren.SetPropertyBlock(_mpb);
        }

        foreach (MeshRenderer ren in _inkTankRender)
        {
            if (ren.name.Contains("M_BombLine") || ren.name.Contains("M_Glass") || ren.name.Contains("M_Ink"))
            {
                ren.GetPropertyBlock(_mpb);
                _mpb.SetFloat("_DitherAlpha", dither);
                ren.SetPropertyBlock(_mpb);
            }
            else
            {
                ren.GetPropertyBlock(_mpb);
                _mpb.SetColor("_BaseColor", new Color(1, 1, 1, alpha));
                ren.SetPropertyBlock(_mpb);
            }
        }
    }

    

    void UpdateInkRefillRender(in RenderState s)
    {
        foreach (MeshRenderer ren in _inkTankRender)
        {
            if (ren.name.Contains("M_BombLine") || ren.name.Contains("M_Glass") || ren.name.Contains("M_Ink"))
            {
                if (ren.name.Contains("M_Ink"))
                {
                    ren.GetPropertyBlock(_mpb);
                    _mpb.SetVector("_Offset", new Vector2(0, s.inktankOffset));
                    ren.SetPropertyBlock(_mpb);
                }
            }

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

    void InkTankRender(bool isOn)
    {
        foreach (var r in _inkTankRender) r.enabled = isOn;
    }
}

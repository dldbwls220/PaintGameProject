using DefineEnum;
using System.Collections;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class InklingController : CharBase
{
    [Header("Test")]
    [Space]


    Transform _followCam;
    Transform _aimTarget;

    [Header("Animation & Visuals")]
    [SerializeField] Animator[] _anim;
    [SerializeField] ParticleSystem _paintParticle;
    [SerializeField] MultiAimConstraint _multiAC;
    [SerializeField] RigBuilder _rigBuilder;
    [Space]

    CharacterController _charController;
    SkinnedMeshRenderer[] _inkingRender;
    SkinnedMeshRenderer[] _halfRender;
    SkinnedMeshRenderer[] _squidRender;
    MeshRenderer[] _inkTankRender;
    MaterialPropertyBlock _inklingMPB;

    [Header("Reference")]
    [SerializeField] InkHit _attackOwner;
    [Space]

    InkTankComponent _inktank;
    [SerializeField] WeaponComponent _shoot; //test
    CharacterClothChanger _clothChanger;
    WallClimbing _wallClimb;

    [Header("GameObject")]
    [SerializeField] GameObject[] _modelObj;
    [SerializeField] GameObject _inkTankObj;
    [SerializeField] GameObject _mouseTarget;
    [SerializeField] GameObject _shootRoot;
    [Space]

    //정보 변수
    AniState _nowState;
    FormState _nowForm;

    [Header("CharacterSetting")]
    [SerializeField] CustomizeState _customizeState;
    [SerializeField] float _inkTankOffset;
    [SerializeField] float _jumpAccel = 4f;
    [SerializeField] float _jumpTurnDecel = 2f;
    [SerializeField] float _swimAccel = 20f;
    [SerializeField] float _swimTurnDecel = 2f;
    [SerializeField] float _gravityForce;
    [Space]

    [Header("CameraSetting")]
    [SerializeField] float _maxAlpha;
    [SerializeField] float _minAlpha;
    [SerializeField] float _upperThreshold = 35f;
    [SerializeField] float _lowerThreshold = -35f;
    [Space]

    float _currentWeight;
    float _tempSpeed;

    bool _isShooting;
    bool _isGround;
    bool _isJumping;
    bool _isClimbing;
    bool _isSwimming;
    bool _isSquid;
    bool _isMorphing;
    bool _isSameColor;
    bool _isOnPaint;
    bool _footSwitch;

    Vector3 _finalMove;
    Vector3 _currentVelocity;
    Vector3 _dir;
    Vector3 _gravity;

    public float _inkOffset { get { return _inkTankOffset; } set {  _inkTankOffset = value; } }
    public float _inkRatio { get { return _nowInk / _totalInk; } }
    public float _hpRatio { get { return _currentHP /  _maxHp; } } 
    public bool _nowSquid { get { return _isSquid; } }
    public bool _nowSameColor { get { return _isSameColor; } }
    public bool _nowOnPaint { get { return _isOnPaint; } }
    public bool _nowJump { get { return _isJumping; } }
    public bool _nowClimb { get { return _isClimbing; } }

    void Start()
    {
        // NetworkInklingMovement가 있으면 Spawned()에서 초기화를 담당
        if (GetComponent<NetworkInklingMovement>() == null)
            InitCharacter("sam");
    }

    public void InitCharacter(string name)
    {
        _charController = GetComponent<CharacterController>();
        _inktank = GetComponent<InkTankComponent>();
        _clothChanger = GetComponent<CharacterClothChanger>();
        _wallClimb = GetComponent<WallClimbing>();

        _followCam = Camera.main.transform;
        InitSetBase(name, 2, 5, 100, 100, 15, 25, _anim);
        _isSquid = false;
        _isSameColor = true;
        _tempSpeed = _runSpeed;
        _inkTankOffset = 0;

        AddAimSource();
        _clothChanger.SetCustomization();

        _inkingRender = _modelObj[(int)FormState.Inkling].GetComponentsInChildren<SkinnedMeshRenderer>();
        _halfRender = _modelObj[(int)FormState.Half].GetComponentsInChildren<SkinnedMeshRenderer>();
        _squidRender = _modelObj[(int)FormState.Squid].GetComponentsInChildren<SkinnedMeshRenderer>();
        _inkTankRender = _inkTankObj.GetComponentsInChildren<MeshRenderer>();

        _inklingMPB = new MaterialPropertyBlock();

        InitTeamColor();
        SwitchRender(FormState.Half, false);
        SwitchRender(FormState.Squid, false);

        _currentWeight = 0;
    }

    void Update()
    {
        MoveInput();

        int targetWeight;

        if (Input.GetKey(KeyCode.LeftShift) && !_isMorphing && _isSameColor)
        {
            ExchangeAnimation(AniState.Morph_toSquid);
            _inktank.RefillInk();
        }
        else if((Input.GetKeyUp(KeyCode.LeftShift) && !_isMorphing) || !_isSameColor)
        {
            ExchangeAnimation(AniState.Morph_toHuman);
        }

        if (Input.GetMouseButton(0) && !_isSquid)
        {
            targetWeight = 1;
            _currentWeight = Mathf.MoveTowards(_currentWeight, targetWeight, 8 * Time.deltaTime);
            _multiAC.weight = 1;
            _isShooting = true;
            _aniController[(int)FormState.Inkling].SetBool("isShooting", true);
            _aniController[(int)FormState.Inkling].SetLayerWeight(1, _currentWeight);

            _shoot.ShootingPaint(_isShooting);         


        }
        else
        {
            targetWeight = 0;
            _currentWeight = Mathf.MoveTowards(_currentWeight, targetWeight, 8 * Time.deltaTime);
            _multiAC.weight = 0;
            _isShooting = false;
            _aniController[(int)FormState.Inkling].SetBool("isShooting", false);
            _aniController[(int)FormState.Inkling].SetLayerWeight(1, _currentWeight);

            _shoot.ShootingPaint(_isShooting);


        }              

        if (!_isMorphing && _isGround)
        {
            if (_dir.magnitude == 0)
            {
                if (_isSameColor)
                {
                    if (!_isSquid)
                        ExchangeAnimation(AniState.Idle);
                    else
                        ExchangeAnimation(AniState.Squid_Idle);
                }
                else
                    ExchangeAnimation(AniState.Slowed);
            }
            else
            {
                if (!_isSquid)
                {
                    if (_isSameColor)
                        ExchangeAnimation(AniState.Run);
                    else
                        ExchangeAnimation(AniState.Slowed_Walk);
                }
                else
                    ExchangeAnimation(AniState.Squid_Walk);
            }
        }

        float angle = _followCam.eulerAngles.x;
        if (angle > 180) angle -= 360;

        AngleTransparency(angle);
        CheckPaintColor();
        PaintColorStatus();
    }

    void MoveInput()
    {
        float mz = Input.GetAxis("Vertical");
        float mx = Input.GetAxis("Horizontal");

        SetAniDirection(mx, mz);

        _inktank.UpdateInkTank(_inkRatio);

        _dir = new Vector3(mx, 0, mz);
        _dir = _dir.magnitude > 1 ? _dir.normalized : _dir;

        Vector3 camForward = _followCam.forward;
        Vector3 camRight = _followCam.right;

        camForward.y = 0;
        camRight.y = 0;

        camForward.Normalize();
        camRight.Normalize();

        _dir = camForward * mz + camRight * mx;
        _dir.Normalize();

        Vector3 targetVelocity = _dir * _runSpeed;

        float dot = Vector3.Dot(_currentVelocity.normalized, targetVelocity.normalized);

        float jumpAccel = _jumpAccel;
        float swimAccel = _swimAccel;

        if (dot < 0.5f)
        {
            jumpAccel = _jumpTurnDecel;
            swimAccel = _swimTurnDecel;
        }

        if (_isShooting) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(camForward), 20f * Time.deltaTime);

        if (_dir.magnitude > 0)
        {
            if (!_isShooting)
            {
                Quaternion targetRot = Quaternion.LookRotation(_dir);

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
            }
        }

        if (_charController.isGrounded) //isGrounded로 지면에 있는지 확인
        {
            _isJumping = false;
            _isGround = true;

            // 땅에 있을 경우 중력을 약하게 줘 충동이 위 아래로 움직이는 걸 방지
            _gravityForce = 0;
            _finalMove.y = -2;

            if(_isSwimming)
            {
                _currentVelocity = Vector3.Lerp(_currentVelocity, targetVelocity, swimAccel * Time.deltaTime);
            }
            else
                _currentVelocity = targetVelocity;

            if (Input.GetKey(KeyCode.Space))
            {
                _finalMove.y = 5f;
                _isJumping = true;
                _footSwitch = !_footSwitch;
                //ExchangeAnimation(AniState.Jump);
            }
        }
        else
        {
            _isGround = false;
            _currentVelocity = Vector3.Lerp(_currentVelocity, targetVelocity, jumpAccel * Time.deltaTime);
            _gravityForce = Physics.gravity.y;
            if (_isSquid)
                ExchangeAnimation(AniState.Squid_Jump);
            else
                ExchangeAnimation(AniState.Jump);
        }
        
        
        if(_wallClimb._wall && _nowSquid)
        {
            Vector3 up = _wallClimb.ClimbingWall();
            _finalMove.y = up.y;
            _finalMove.y += -1;

            _isClimbing = true;

            Debug.Log("벽타기");
        }
        else _isClimbing = false;

            _finalMove.x = _currentVelocity.x;
        _finalMove.z = _currentVelocity.z;
        _finalMove.y += _gravityForce * Time.deltaTime; 

        _charController.Move(_finalMove * Time.deltaTime); // SimpleMove는 중력을 자동으로 적용을 하며 테스트를 할 때 사용 Move는 모든적 직접 제어

        Debug.Log("점프중 : " + _isJumping);
    }


    void SetAniDirection(float x, float z)
    {
        _aniController[(int)FormState.Inkling].SetFloat("RNL", x);
        _aniController[(int)FormState.Inkling].SetFloat("FNB", z);

        _aniController[(int)FormState.Half].SetFloat("RNL", x);
        _aniController[(int)FormState.Half].SetFloat("FNB", z);
    }

    public override void ExchangeAnimation(AniState state)
    {
        switch (state)
        {
            case AniState.Idle:
                _aniController[(int)FormState.Inkling].SetBool("isGround", _isGround);
                _aniController[(int)FormState.Inkling].SetBool("isJumping", _nowJump);
                break;
            case AniState.Walk:
                _aniController[(int)FormState.Inkling].SetBool("isGround", _isGround);
                _aniController[(int)FormState.Inkling].SetBool("isJumping", _nowJump);
                break;
            case AniState.Run:
                _aniController[(int)FormState.Inkling].SetBool("isSlowed", false);
                _aniController[(int)FormState.Half].SetBool("isSlowed", false);
                _aniController[(int)FormState.Inkling].SetBool("isGround", _isGround);
                _aniController[(int)FormState.Inkling].SetBool("isJumping", _nowJump);
                break;
            case AniState.Jump:
                _aniController[(int)FormState.Inkling].SetBool("FootSwitch", _footSwitch);
                _aniController[(int)FormState.Inkling].SetBool("isGround", _isGround);
                _aniController[(int)FormState.Inkling].SetBool("isJumping", _nowJump);             
                break;
            case AniState.Shoot:
                break;
            case AniState.Morph_toSquid:
                if (!_isSquid)
                {                    
                    _aniController[(int)FormState.Inkling].SetBool("isSquid", true);
                    _aniController[(int)FormState.Half].SetBool("isSquid", true);
                    StartCoroutine(MorphToSquid((int)state));
                }
                break;
            case AniState.Morph_toHuman:
                if (_isSquid)
                {
                    _aniController[(int)FormState.Inkling].SetBool("isSquid", false);
                    _aniController[(int)FormState.Half].SetBool("isSquid", false);
                    StartCoroutine(MorphToInkling((int)state));
                }
                break;
            case AniState.Slowed:
                _aniController[(int)FormState.Inkling].SetBool("isSlowed", true);
                _aniController[(int)FormState.Half].SetBool("isSlowed", true);
                break;
            case AniState.Slowed_Walk:
                _aniController[(int)FormState.Inkling].SetBool("isSlowed", true);
                _aniController[(int)FormState.Half].SetBool("isSlowed", true);
                break;
            case AniState.Squid_Idle:
                _aniController[(int)FormState.Squid].SetBool("isGround", _isGround);
                _aniController[(int)FormState.Squid].SetBool("isJumping", _nowJump);
                break;
            case AniState.Squid_Walk:
                _aniController[(int)FormState.Squid].SetBool("isGround", _isGround);
                _aniController[(int)FormState.Squid].SetBool("isJumping", _nowJump);
                break;
            case AniState.Squid_Jump:
                _aniController[(int)FormState.Squid].SetBool("isGround", _isGround);
                _aniController[(int)FormState.Squid].SetBool("isJumping", _nowJump);
                break;
        }

        

        if (_isSquid)
        {
            _aniController[(int)FormState.Squid].SetInteger("AniState", (int)state);
        }
        else
        {
            _aniController[(int)FormState.Inkling].SetInteger("AniState", (int)state);
            _aniController[(int)FormState.Half].SetInteger("AniState", (int)state);
        }

            _nowState = state;

        Debug.Log(state.ToString());
    }

    void SwitchRender(FormState state, bool isOn)
    {
        switch (state)
        {
            case FormState.Inkling:
                for (int i = 0; i < _inkingRender.Length; i++)
                {
                    _inkingRender[i].enabled = isOn;
                }

                for (int i = 0; i < _inkTankRender.Length; i++)
                {
                    _inkTankRender[i].enabled = isOn;
                }

                    break;
            case FormState.Half:
                for (int i = 0; i < _halfRender.Length; i++)
                {
                    _halfRender[i].enabled = isOn;
                }
                break;
            case FormState.Squid:
                for (int i = 0; i < _squidRender.Length; i++)
                {
                    _squidRender[i].enabled = isOn;
                }
                break;
        }
        
    }

    void AngleTransparency(float angle)
    {
        float alpha = 1;
        float dither = 0;

        if (angle > _upperThreshold)
        {
            float range = 70f - _upperThreshold;
            float progress = (angle - _upperThreshold) / range;
            alpha = Mathf.Lerp(_maxAlpha, _minAlpha, progress);
            dither = Mathf.Lerp(_minAlpha, _maxAlpha, progress);
        }
        else if (angle < _lowerThreshold)
        {
            float range = Mathf.Abs(-70f -_lowerThreshold);
            float progress = (_lowerThreshold - angle) / range;
            alpha = Mathf.Lerp(_maxAlpha, _minAlpha, progress);
            dither = Mathf.Lerp(_minAlpha, _maxAlpha, progress);
        }

        foreach (SkinnedMeshRenderer ren in _inkingRender)
        {
            if (ren.name.Contains("_TeamC"))
            {
                ren.GetPropertyBlock(_inklingMPB);
                _inklingMPB.SetColor("_BaseColor", new Color(_teamColor.r, _teamColor.g, _teamColor.b, alpha));
                ren.SetPropertyBlock(_inklingMPB);
            }
            else
            {
                ren.GetPropertyBlock(_inklingMPB);
                _inklingMPB.SetColor("_BaseColor", new Color(1, 1, 1, alpha));
                ren.SetPropertyBlock(_inklingMPB);
            }          
        }

        foreach (MeshRenderer ren in _inkTankRender)
        {
            if (ren.name.Contains("M_BombLine") || ren.name.Contains("M_Glass") || ren.name.Contains("M_Ink"))
            {
                ren.GetPropertyBlock(_inklingMPB);
                _inklingMPB.SetFloat("_DitherAlpha", dither);
                ren.SetPropertyBlock(_inklingMPB);

                if (ren.name.Contains("M_Ink"))
                {
                    ren.GetPropertyBlock(_inklingMPB);
                    _inklingMPB.SetVector("_Offset", new Vector2(0, _inkOffset));
                    ren.SetPropertyBlock(_inklingMPB);
                }
            }
            else
            {
                ren.GetPropertyBlock(_inklingMPB);
                _inklingMPB.SetColor("_BaseColor", new Color(1, 1, 1, alpha));
                ren.SetPropertyBlock(_inklingMPB);
            }
        }
    }

    void InitTeamColor()
    {
        foreach (SkinnedMeshRenderer ren in _inkingRender)
        {
            if (ren.name.Contains("_TeamC"))
            {
                ren.GetPropertyBlock(_inklingMPB);
                _inklingMPB.SetColor("_BaseColor", _teamColor);
                ren.SetPropertyBlock(_inklingMPB);
            }

            if (ren.name.Contains("_TeamE"))
            {
                ren.GetPropertyBlock(_inklingMPB);
                _inklingMPB.SetColor("_EmissionColor", _teamColor);
                ren.SetPropertyBlock(_inklingMPB);
            }
        }

        foreach (SkinnedMeshRenderer ren in _halfRender)
        {
            if (ren.name.Contains("_TeamC"))
            {
                ren.GetPropertyBlock(_inklingMPB);
                _inklingMPB.SetColor("_BaseColor", _teamColor);
                ren.SetPropertyBlock(_inklingMPB);
            }
            else if (ren.name.Contains("_TeamE"))
            {
                ren.GetPropertyBlock(_inklingMPB);
                _inklingMPB.SetColor("_EmissionColor", _teamColor);
                ren.SetPropertyBlock(_inklingMPB);
            }
        }

        foreach (SkinnedMeshRenderer ren in _squidRender)
        {
            if (ren.name.Contains("_TeamC"))
            {
                ren.GetPropertyBlock(_inklingMPB);
                _inklingMPB.SetColor("_BaseColor", _teamColor);
                _inklingMPB.SetColor("_EmissionColor", _teamColor);
                ren.SetPropertyBlock(_inklingMPB);
            }
        }

        foreach (MeshRenderer ren in _inkTankRender)
        {
            if (ren.name.Contains("M_Ink"))
            {
                ren.GetPropertyBlock(_inklingMPB);
                _inklingMPB.SetColor("_BaseColor", _teamColor);
                ren.SetPropertyBlock(_inklingMPB);
            }
        }
    }

    void CheckPaintColor()
    {
        if (_charController.isGrounded)
        {
            Ray ray = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);
            // 1. 캐릭터 위치에서 아래로 레이를 쏴서 UV 좌표를 찾음
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Paintabale paintable = hit.collider.GetComponentInParent<Paintabale>();

                WorldInkReceiver receiver = hit.collider.GetComponent<WorldInkReceiver>();

                if (paintable != null)
                {
                    // 3. 네가 만든 메서드 호출!
                    Color groundColor = paintable.CheckPaintColor(hit);

                    // 4. 결과 활용
                    CheckFloorStatus(groundColor);
                }

                if (receiver != null)
                {
                    Color inkcolor = receiver.CheckPaintColor(hit);

                    CheckFloorStatus(inkcolor);
                }
            }

            _isJumping = false;
        }
        else { _isJumping = true; }       
    }

    void CheckFloorStatus(Color col)
    {
        //알파값이 낮으면 잉크가 없는 곳 [cite: 22, 23]
        if (col.a < 0.1f)
        {
            _isSameColor = true;
            _isOnPaint = false;
            Debug.Log("NotOnPaint");
        }
        else
        {
            Debug.Log("OnPaint");
            _isOnPaint = true;
        }

        float distToMyTeam = Mathf.Abs(col.r - _teamColor.r) + Mathf.Abs(col.g - _teamColor.g) + Mathf.Abs(col.b - _teamColor.b);

        float distToEnemyTeam = Mathf.Abs(col.r - _enemyColor.r) + Mathf.Abs(col.g - _enemyColor.g) + Mathf.Abs(col.b - _enemyColor.b);

        if (distToMyTeam < distToEnemyTeam && distToMyTeam < 0.5f)
        {
            _isSameColor = true;
            Debug.Log("우리 팀 구역입니다!");
        }
        else if (distToEnemyTeam < distToMyTeam && distToEnemyTeam < 0.5f)
        {
            _isSameColor = false;
            Debug.Log("상대 팀 구역입니다!");
        }
    }

    void PaintColorStatus()
    {
        if (_isOnPaint && !_isJumping)
        {
            if (_isSquid) SwitchRender(FormState.Squid, false);
        }
        else
        {
            if (_isSquid && !_isMorphing) SwitchRender(FormState.Squid, true);
            else if (_isSquid && _isMorphing)
                SwitchRender(FormState.Squid, false);

            _runSpeed = _tempSpeed;
            _isSwimming = false;
        }
        


        if (_isSameColor)
        {
            if (!_isSquid)
            {
                _runSpeed = _tempSpeed;
                _isSwimming = false;
            }
            else if (_isSquid && _isOnPaint)
            {
                _runSpeed = _tempSpeed * 1.5f;
                _isSwimming = true;
            }
        }
        else
        {
            _runSpeed = _tempSpeed * 0.2f;
            _isSwimming = false;
        }
    }

    void AddAimSource()
    {
        var sourceObj = _multiAC.data.sourceObjects;

        var newsource = new WeightedTransform(_mouseTarget.transform, 1);
        sourceObj.Add(newsource);

        _multiAC.data.sourceObjects = sourceObj;

        if (_rigBuilder != null)
        {
            _rigBuilder.Build();
        }
    }

    public void GetDamage(float dmg)
    {
        if ((_currentHP -= dmg) >= 0)
        {
            _currentHP = 0;
        }
        else
        {

        }
    }

    IEnumerator MorphToSquid(int state)
    {
        _isMorphing = true;

        SwitchRender(FormState.Inkling, false);
        SwitchRender(FormState.Half, true);

        _aniController[(int)FormState.Inkling].SetInteger("AniState", state);
        _aniController[(int)FormState.Half].SetInteger("AniState", state);

        yield return new WaitForSeconds(0.1f);

        SwitchRender(FormState.Half, false);
        SwitchRender(FormState.Squid, true);

        _isSquid = true;
        _isMorphing = false;
    }

    IEnumerator MorphToInkling(int state)
    {
        _isMorphing = true;

        SwitchRender(FormState.Half, true);
        SwitchRender(FormState.Squid, false);

        _aniController[(int)FormState.Inkling].SetInteger("AniState", state);
        _aniController[(int)FormState.Half].SetInteger("AniState", state);

        _aniController[(int)FormState.Inkling].SetTrigger("ToHuman");
        _aniController[(int)FormState.Half].SetTrigger("ToHuman");

        yield return new WaitForSeconds(0.2f);

        SwitchRender(FormState.Inkling, true);
        SwitchRender(FormState.Half, false);

        _isSquid = false;
        _isMorphing = false;
    }
}

using UnityEngine;
using Fusion;

public class WeaponManager : NetworkBehaviour
{
    [Header ("Class Reference")]
    [SerializeField] InkTankController _inkTankC;
    [SerializeField] ProjectileSoundManager _soundManger;

    [Header("Weapon Setup")]
    Transform _inkShootTF;
    public Transform _target;
    public ParticleSystem _shootFX;
    public Color _inkColor;
    public float _totalInk;
    public float _fillSpeed;
    public float _autoFillSpeed;
    public Weapon[] _allWeapons;

    [HideInInspector]
    public bool _refillable;
    public bool _isEmpty;
    public bool _canCharge;
    public bool _isCharging;

    [Networked] public float _inktankOffset { get; set; } = 0;
    [Networked, HideInInspector] public Weapon _currentWeapon { get; set; }

    [Networked] public float _currentInk { get; set; }

    public float _distance {  get; private set; }

    public struct InkTankState
    {
        public bool isSquid;
        public bool isSameColor;
        public bool isJumping;
        public bool isSwimming;
    }

    public void InitWeapon()
    {
        _allWeapons = GetComponentsInChildren<Weapon>();
        _currentWeapon = _allWeapons[0]; //임시
        _distance = _currentWeapon._distance;
    }

    public void Init(Color color, Transform target, float totalInk, Transform root)
    {
        _inkTankC.Init();
        _inkColor = color;
        _target = target.GetChild(0);
        _totalInk = totalInk;
        _currentInk = _totalInk;
        _inkShootTF = root.transform.GetChild(0);

        _soundManger.LoadAllSound();

        _allWeapons = GetComponentsInChildren<Weapon>();

        var main = _shootFX.main;
        main.startColor = _inkColor;
    }

    public void Shoot(bool isShootPressed)
    {
        _currentWeapon?.Shoot(_inkShootTF, _target.position, _inkColor, isShootPressed, _isEmpty);
        _inkTankC.UpdateInkTank(_currentInk / _totalInk);
    }

    public void UpdateInkStatus(in InkTankState s)
    {
        CheckInkStatus(s);

        if (HasInputAuthority)
            _inkTankC.OnOffInkTank(s.isSquid);
    }

    public void UpdateShootSound()
    {
        _currentWeapon?.UpdateShootSound();
    }

    public float UpdateInktankOffset()
    {
        return _inktankOffset;
    }

    void CheckInkStatus(in InkTankState s)
    {
        _canCharge =
       ((s.isSquid &&
       s.isSameColor &&
       !s.isJumping &&
       s.isSwimming));

        if(_currentInk <= 0) _isEmpty = true;
        else _isEmpty = false;
    }

    public void AutoRefill(bool isShooting)
    {
        if (isShooting || _currentInk >= _totalInk || _isCharging) return;

        float fillSpeed = _totalInk / _autoFillSpeed;
        _currentInk = Mathf.MoveTowards(_currentInk, _totalInk, fillSpeed * Runner.DeltaTime);

        float offsetSpeed = fillSpeed * (0.5f / _totalInk);
        _inktankOffset = Mathf.MoveTowards(_inktankOffset, 0, offsetSpeed * Runner.DeltaTime);

        _inkTankC.UpdateInkTank(_currentInk / _totalInk);

        Debug.Log("자동충전");
    }

    public void RefillInk()
    {
        if (!_canCharge || _currentInk >= _totalInk)
        {
            _isCharging = false;
            return;
        }

        _isCharging = true;

        float fillSpeed = _totalInk / _fillSpeed;
        _currentInk = Mathf.MoveTowards(_currentInk, _totalInk, fillSpeed * Runner.DeltaTime);

        float offsetSpeed = fillSpeed * (0.5f / _totalInk);
        _inktankOffset = Mathf.MoveTowards(_inktankOffset, 0, offsetSpeed * Runner.DeltaTime);

        _inkTankC.UpdateInkTank(_currentInk / _totalInk);
    }

    public void PlayShootFX()
    {
        _shootFX.Play();
    }

    
}

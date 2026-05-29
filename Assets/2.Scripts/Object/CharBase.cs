using DefineEnum;
using UnityEngine;

public abstract class CharBase : MonoBehaviour
{
    protected Animator[] _aniController;
    protected Color _teamColor;
    protected Color _enemyColor;
    protected string _name;
    protected float _walkSpeed;
    protected float _runSpeed;
    protected bool _isDeath;

    protected float _maxHp;
    protected float _currentHP;
    protected float _totalInk;
    protected float _nowInk;
    protected float _inkUseRate;
    protected float _dmg;

    public string _myName { get { return _name; } }
    public float _maxInk { get { return _totalInk; } } 
    public float _currentInk { get { return _nowInk;} set { _nowInk = value; } }
    public bool _isDead { get { return _isDeath; } }
    public Color _myColor { get { return _teamColor; } }
    public Color _otherColor { get { return _enemyColor; } }

    protected void InitSetBase(string name, float walkSpeed, float runSpeed, float maxHp, float maxInk, float inkRate, float dmg, Animator[] anim, Color? color = null)
    {
        _name = name;
        _walkSpeed = walkSpeed;        
        _runSpeed = runSpeed;
        _teamColor = color ?? Color.navyBlue;
        _enemyColor = Color.darkRed;
        _maxHp = maxHp;
        _currentHP = _maxHp;
        _totalInk = maxInk;
        _nowInk = _totalInk;
        _inkUseRate = inkRate;
        _dmg = dmg;

        _aniController = anim;
    }

    public abstract void ExchangeAnimation(AniState state);

    public virtual void CheckedOpponent(GameObject hostileChar)
    {
        Debug.Log("상대 감지");
    }
}

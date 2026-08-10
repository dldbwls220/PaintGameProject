using UnityEngine;
using UnityEngine.UI;
using DefineEnum;

public class PartSelectUI : MonoBehaviour
{
    [Header("Item Image")]
    [SerializeField] Image _headImg;
    [SerializeField] Image _clothImg;
    [SerializeField] Image _shoesImg;

    [Header("Wnd Anim")]
    [SerializeField] Animation _headAnim;
    [SerializeField] Animation _clothAnim;
    [SerializeField] Animation _shoesAnim;
    [SerializeField] Animation _styleAnim;
    [SerializeField] Animation _nameAnim;
    AnimationState _headState;
    AnimationState _clothState;
    AnimationState _shoesState;
    AnimationState _styleState;
    AnimationState _nameState;

    bool _isHeadOpen;
    bool _isShoesOpen;
    bool _isStyleOpen;
    bool _isClothOpen;
    bool _isNameOpen;

    bool _wasHeadOpen;
    bool _wasShoesOpen;
    bool _wasStyleOpen;
    bool _wasClothOpen;
    bool _wasNameOpen;

    private void Start()
    {
        _headState = _headAnim[_headAnim.clip.name];
        _clothState = _clothAnim[_clothAnim.clip.name];
        _shoesState = _shoesAnim[_shoesAnim.clip.name];
        _styleState = _styleAnim[_styleAnim.clip.name];
        _nameState = _nameAnim[_nameAnim.clip.name];

        OpenHeadGearWnd();
    }

    private void Update()
    {
        CheckItemImage();

        playAnimation();
    }

    void CheckItemImage()
    {
        string name = PlayerCustomizeManager.instance.Customization._head.ToString();

        _headImg.sprite = ResourcePoolManager.instance.Get<Sprite>(PoolDataType.HEADGEARIMG, name);

        name = PlayerCustomizeManager.instance.Customization._cloth.ToString();

        _clothImg.sprite = ResourcePoolManager.instance.Get<Sprite>(PoolDataType.CLOTHGEARIMG, name);

        name = PlayerCustomizeManager.instance.Customization._shoes.ToString();

        _shoesImg.sprite = ResourcePoolManager.instance.Get<Sprite>(PoolDataType.SHOESGEARIMG, name);
    }

    #region[WndAnim]

    void PlayHeadGearAnim()
    {
        _headState.speed = 1;
        _headState.time = 0;
        _headAnim.Play();
    }

    void PlayClothGearAnim()
    {
        _clothState.speed = 1;
        _clothState.time = 0;
        _clothAnim.Play();
    }

    void PlayShoesGearAnim()
    {
        _shoesState.speed = 1;
        _shoesState.time = 0;
        _shoesAnim.Play();
    }

    void PlayStyleAnim()
    {
        _styleState.speed = 1;
        _styleState.time = 0;
        _styleAnim.Play();
    }

    void PlayNameAnim()
    {
        _nameState.speed = 1;
        _nameState.time = 0;
        _nameAnim.Play();
    }

    void ReverseHeadGearAnim()
    {
        _headState.speed = -1;
        _headState.time = _headState.length;
        _headAnim.Play();
    }

    void ReverseClothGearAnim()
    {
        _clothState.speed = -1;
        _clothState.time = _clothState.length;
        _clothAnim.Play();
    }

    void ReverseShoesGearAnim()
    {
        _shoesState.speed = -1;
        _shoesState.time = _shoesState.length;
        _shoesAnim.Play();
    }
    void ReverseStyleAnim()
    {
        _styleState.speed = -1;
        _styleState.time = _styleState.length;
        _styleAnim.Play();
    }

    void ReverseNameAnim()
    {
        _nameState.speed = -1;
        _nameState.time = _styleState.length;
        _nameAnim.Play();
    }

    #endregion[WndAnim]

    public void OpenHeadGearWnd()
    {
        _isShoesOpen = false;
        _isClothOpen = false;
        _isStyleOpen = false;
        _isHeadOpen = true;
    }

    public void OpenClothGearWnd()
    {
        _isShoesOpen = false;
        _isClothOpen = true;
        _isStyleOpen = false;
        _isHeadOpen = false;
        _isNameOpen = false;
    }

    public void OpenShoesGearWnd()
    {
        _isShoesOpen = true;
        _isClothOpen = false;
        _isStyleOpen = false;
        _isHeadOpen = false;
        _isNameOpen = false;
    }

    public void OpenStyleWnd()
    {
        _isShoesOpen = false;
        _isClothOpen = false;
        _isStyleOpen = true;
        _isHeadOpen = false;
        _isNameOpen = false;
    }

    public void OpenNameWnd()
    {
        _isShoesOpen = false;
        _isClothOpen = false;
        _isStyleOpen = false;
        _isHeadOpen = false;
        _isNameOpen = true;
    }

    void playAnimation()
    {
        if (_wasClothOpen && !_isClothOpen)
        {
            ReverseClothGearAnim();
        }
        else if (!_wasClothOpen && _isClothOpen)
        {
            PlayClothGearAnim();
        }

        if (_wasHeadOpen && !_isHeadOpen)
        {
            ReverseHeadGearAnim();
        }
        else if (!_wasHeadOpen && _isHeadOpen)
        {
            PlayHeadGearAnim();
        }

        if (_wasShoesOpen && !_isShoesOpen)
        {
            ReverseShoesGearAnim();
        }
        else if (!_wasShoesOpen && _isShoesOpen)
        {
            PlayShoesGearAnim();
        }

        if (_wasStyleOpen && !_isStyleOpen)
        {
            ReverseStyleAnim();
        }
        else if (!_wasStyleOpen && _isStyleOpen)
        {
            PlayStyleAnim();
        }

        if (_wasNameOpen && !_isNameOpen)
        {
            ReverseNameAnim();
        }
        else if (!_wasNameOpen && _isNameOpen)
        {
            PlayNameAnim();
        }

        _wasHeadOpen = _isHeadOpen;
        _wasClothOpen = _isClothOpen;
        _wasShoesOpen = _isShoesOpen;
        _wasStyleOpen = _isStyleOpen;
        _wasNameOpen = _isNameOpen;

    }

    public void PlayPartSelect()
    {
        GameSoundManager.instance.PlayerSFX(PlayerSFXName.CustomizeUI_Decide, volume: 0.6f);
    }
}

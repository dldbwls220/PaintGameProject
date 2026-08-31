using UnityEngine;
using UnityEngine.UI;
using DefineEnum;

public class ItemSlotUI : MonoBehaviour
{
    [SerializeField] Image _itemImg;
    [SerializeField] Image _backGround;
    [SerializeField] Animation _animation;
    AnimationState _state;

    [Header("For Debuging")]
    [SerializeField] BodyState _bodyState;
    [SerializeField] HeadState _headState;
    [SerializeField] ShoeState _shoeState;

    bool _isSelected;
    bool _wasSelected;

    private void Update()
    {
        _isSelected = IsCurrentlySelected();

        if (_isSelected && !_wasSelected)
        {
            PlayAnimation();
        }
        else if (!_isSelected && _wasSelected)
        {
            ReverseAnimation();
        }
            _wasSelected = _isSelected;
    }

    public void InitWnd(BodyState body = BodyState.Count, HeadState head = HeadState.Count, ShoeState shoe = ShoeState.Count)
    {
        Sprite icon = null;

        if (body != BodyState.Count)
        {
            icon = ResourcePoolManager.instance.Get<Sprite>(PoolDataType.CLOTHGEARIMG, body.ToString());
        }
        else if (head != HeadState.Count)
        {
            icon = ResourcePoolManager.instance.Get<Sprite>(PoolDataType.HEADGEARIMG, head.ToString());
        }
        else if (shoe != ShoeState.Count)
        {
            icon = ResourcePoolManager.instance.Get<Sprite>(PoolDataType.SHOESGEARIMG, shoe.ToString());
        }

        _bodyState = body; _headState = head; _shoeState = shoe;

        _itemImg.sprite = icon;

        _state = _animation[_animation.clip.name];

        _isSelected = IsCurrentlySelected();
        _wasSelected = _isSelected;

        if (_isSelected)
        {
            SnapToSelectedState();
        }
    }

    bool IsCurrentlySelected()
    {
        return PlayerCustomizeManager.instance.Customization._head == _headState
            || PlayerCustomizeManager.instance.Customization._cloth == _bodyState
            || PlayerCustomizeManager.instance.Customization._shoes == _shoeState;
    }

    void SnapToSelectedState()
    {
        _state.speed = 1;
        _state.time = _state.length;
        _animation.Play();
        _animation.Sample();
        _animation.Stop();
    }

    public void SetItem()
    {
        if (_bodyState != BodyState.Count)
        {
            PlayerCustomizeManager.instance.SetBody(_bodyState);
        }
        else if (_headState != HeadState.Count)
        {
            PlayerCustomizeManager.instance.SetHead(_headState);
        }
        else if (_shoeState != ShoeState.Count)
        {
            PlayerCustomizeManager.instance.SetShoe(_shoeState);
        }

        if (CustomizeVisualManager._instance != null)
        {
            CustomizeVisualManager._instance.UpdateClothing();
        }
    }

    public void PlayAnimation()
    {
        _state.speed = 1;
        _state.time = 0;
        _animation.Play();
    }

    public void ReverseAnimation()
    {
        _state.speed = -1;
        _state.time = _state.length;
        _animation.Play();
    }

    public void PlayGesrSelect()
    {
        if (_isSelected) return;
        GameSoundManager.instance.PlayerSFX(PlayerSFXName.UI_Decide00, volume: 0.6f);
    }
}

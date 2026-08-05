using DefineEnum;
using UnityEngine;
using UnityEngine.UI;

public class EyebrowsSlotUI : MonoBehaviour
{
    [Header("Hair Setting")]
    [SerializeField] EyebrowsState _eyebrowaState;
    [SerializeField] Color _selectedColor;

    [Header("Reference")]
    [SerializeField] Image _image;
    [SerializeField] Image _check;
    [SerializeField] Animation _animation;
    AnimationState _state;
    Color _originColor;

    bool _isSelected;
    bool _wasSelected;

    private void Start()
    {
        _image.sprite = ResourcePoolManager.instance.Get<Sprite>(PoolDataType.EYEBROWSICONIMG, _eyebrowaState.ToString());
        _originColor = _image.color;
        _check.enabled = false;

        _state = _animation[_animation.clip.name];

        _isSelected = IsCurrentlySelected();
        _wasSelected = _isSelected;

        if (_isSelected)
        {
            _check.enabled = true;
            SnapToSelectedState();
            OnMouseEyebrow();
        }
    }

    private void Update()
    {
        _isSelected = IsCurrentlySelected();

        if (_isSelected && !_wasSelected)
        {
            OnMouseEyebrow();
            _check.enabled = true;
            PlayAnimation();
        }
        else if (!_isSelected && _wasSelected)
        {
            OutMouseEyebrow();
            _check.enabled = false;
        }
        _wasSelected = _isSelected;
    }

    public void SetEyebrows()
    {
        PlayerCustomizeManager.instance.SetEyebrowa(_eyebrowaState);
    }

    public void OnMouseEyebrow()
    {
        _image.color = _selectedColor;
    }

    public void OutMouseEyebrow()
    {
        if (_isSelected) return;
        _image.color = _originColor;
    }

    void SnapToSelectedState()
    {
        _state.speed = 1;
        _state.time = _state.length;
        _animation.Play();
        _animation.Sample();
        _animation.Stop();
    }

    bool IsCurrentlySelected()
    {
        return PlayerCustomizeManager.instance.Customization._eyebrows == _eyebrowaState;
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

    public void PlayStyleSelect()
    {
        if (_isSelected) return;
        GameSoundManager.instance.PlayerSFX(PlayerSFXName.UI_Decide00, volume: 0.6f);
    }
}

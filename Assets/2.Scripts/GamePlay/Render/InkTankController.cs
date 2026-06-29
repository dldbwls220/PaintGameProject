using UnityEngine;
using UnityEngine.UI;

public class InkTankController : MonoBehaviour
{
    [Header("Ink UI")]
    [SerializeField] Slider _inkSlider;
    [SerializeField] Vector2 _screenOffset = new Vector2(150f, 50f);

    [Header("Ink Setting")]
    [SerializeField] float _maxInk = 100f;
    [SerializeField] float _currentInk;
    [SerializeField] float _fillSpeed;
    [SerializeField] float _inktankOffset;

    RectTransform _rectT;
    Transform[] _sliderChildT;
    bool _canCharge;

    public struct InkTankState
    {
        public bool isSquid;
        public bool isSameColor;
        public bool isJumping;
        public bool isSwimming;
    }

    public void Init()
    {
        _rectT = _inkSlider.GetComponent<RectTransform>();
        _sliderChildT = _inkSlider.GetComponentsInChildren<Transform>();
        _currentInk = _maxInk;
        _inktankOffset = 0;

        foreach (Transform tf in _sliderChildT)
        {
            tf.gameObject.SetActive(false);
        }
    }

    public void UpdateInkStatus(in InkTankState s)
    {
        CheckInkRefillable(s);
        RefillInk();
    }

    public void SetInkUIPos()
    {
        Camera main = Camera.main;

        Vector3 screenPoint = main.WorldToScreenPoint(transform.position);

        if (screenPoint.z < 0)
        {
            _rectT.gameObject.SetActive(false);
            return;
        }
        else
        {
            if (!_rectT.gameObject.activeSelf) _rectT.gameObject.SetActive(true);
        }

        Vector2 finalPosition = new Vector2(screenPoint.x + _screenOffset.x, screenPoint.y + _screenOffset.y);


        _rectT.position = finalPosition;
    }

    public float UpdateInktankOffset()
    {
        return _inktankOffset;
    }

    void CheckInkRefillable(in InkTankState s)
    {
        _canCharge =
       ((s.isSquid &&
       s.isSameColor &&
       !s.isJumping &&
       s.isSwimming));
    }

    void RefillInk()
    {
        if (!_canCharge || _currentInk >= _maxInk)
        {
            return;
        }

        float fillSpeed = _maxInk / _fillSpeed;
        _currentInk = Mathf.MoveTowards(_currentInk, _maxInk, fillSpeed * Time.deltaTime);

        float offsetSpeed = fillSpeed * (0.5f / _maxInk);
        _inktankOffset = Mathf.MoveTowards(_inktankOffset, 0, offsetSpeed * Time.deltaTime);       
    }
}

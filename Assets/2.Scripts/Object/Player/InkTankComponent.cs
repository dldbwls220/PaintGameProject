using UnityEngine;
using UnityEngine.UI;

public class InkTankComponent : MonoBehaviour
{
    InklingController _inkling;    
    [SerializeField] Slider _inktankSlider;
    Camera _mainCam;
    RectTransform _rectT;

    Transform[] _sliderChildT;

    [SerializeField] Vector2 _screenOffset = new Vector2(150f, 50f);

    bool _isCharging;

    bool _canRefillTank =>
        ((_inkling._nowSquid &&
        _inkling._nowSameColor &&
        _inkling._nowOnPaint &&
        !_inkling._nowJump) ||
        _inkling._nowClimb);

    void Start()
    {
        _inkling = GetComponent<InklingController>();
        _mainCam = Camera.main;
        _rectT = _inktankSlider.GetComponent<RectTransform>();
        _sliderChildT = _inktankSlider.GetComponentsInChildren<Transform>();

        foreach (Transform tf in _sliderChildT)
        {
            tf.gameObject.SetActive(false);
        }
        
        _isCharging = false;
    }

    void Update()
    {
        OnOffInkTank();
    }

    void LateUpdate()
    {
        Vector3 screenPoint = _mainCam.WorldToScreenPoint(transform.position);

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

    public void UpdateInkTank(float ink)
    {
        _inktankSlider.value = ink;
    }

    public void RefillInk()
    {
        //if (_inkling._currentInk <= _inkling._maxInk && _canRefillTank)
        //{
        //    _isCharging = true;
        //}

        //if (_inkling._currentInk < _inkling._maxInk && _isCharging)
        //{
        //    float fillSpeed = _inkling._maxInk / 5f;

        //    _inkling._currentInk = Mathf.MoveTowards(_inkling._currentInk, _inkling._maxInk, fillSpeed * Time.deltaTime);

        //    float offsetSpeed = fillSpeed * (0.5f / _inkling._maxInk);

        //    _inkling._inkOffset = Mathf.MoveTowards(_inkling._inkOffset, 0, offsetSpeed * Time.deltaTime);

        //    if (_inkling._currentInk >= _inkling._maxInk || !_inkling._nowSquid || !_inkling._nowSameColor || !_inkling._nowOnPaint || _inkling._nowJump)
        //    {
        //        _isCharging = false;
        //    }
        //}
        if (!_canRefillTank || _inkling._currentInk >= _inkling._maxInk)
        {
            _isCharging = false;
            return;
        }

        // 여기까지 왔다면 무조건 충전 가능한 상태임
        _isCharging = true;

        // 잉크 충전 로직 수행
        float fillSpeed = _inkling._maxInk / 5f;
        _inkling._currentInk = Mathf.MoveTowards(_inkling._currentInk, _inkling._maxInk, fillSpeed * Time.deltaTime);

        float offsetSpeed = fillSpeed * (0.5f / _inkling._maxInk);
        _inkling._inkOffset = Mathf.MoveTowards(_inkling._inkOffset, 0, offsetSpeed * Time.deltaTime);

    }


    void OnOffInkTank()
    {
        if (_inkling._nowSquid && _inkling._nowOnPaint)
        {
            foreach (Transform tf in _sliderChildT)
            {
                tf.gameObject.SetActive(true);
            }
        }
        else if (!_inkling._nowSquid)
        {
            foreach (Transform tf in _sliderChildT)
            {
                tf.gameObject.SetActive(false);
            }
        }
    }
}

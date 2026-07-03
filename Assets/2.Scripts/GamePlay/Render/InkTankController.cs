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

    public void UpdateInkTank(float ink)
    {
        _inkSlider.value = ink;
    }

    public float UpdateInktankOffset()
    {
        return _inktankOffset;
    }

    public void OnOffInkTank(bool isSquid)
    {
        if (isSquid)
        {
            foreach (Transform tf in _sliderChildT)
            {
                tf.gameObject.SetActive(true);
            }
        }
        else if (!isSquid)
        {
            foreach (Transform tf in _sliderChildT)
            {
                tf.gameObject.SetActive(false);
            }
        }
    }
}

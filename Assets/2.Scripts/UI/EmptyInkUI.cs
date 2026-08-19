using UnityEngine;
using UnityEngine.UI;

public class EmptyInkUI : MonoBehaviour
{
    [Header("Blink Anim")]
    [SerializeField] Animation _anim;
    [SerializeField] AnimationClip _blinkClip;

    [Header("Moving Bar")]
    [SerializeField] float _startPos = -1300f;
    [SerializeField] float _endPos = 1300f;
    [SerializeField] float _moveSpeed = 50f;
    [SerializeField] RectTransform[] _barRT;

    [Header("Set Color")]
    [SerializeField] Image _frame;
    [SerializeField] Image _bg;
    [SerializeField] Image[] _movingBar;

    bool _isOn;
    Vector2[] _originPos;

    void Update()
    {
        MoveBar();
    }

    public void OpenWnd()
    {
        gameObject.SetActive(true);
    }

    public void InitEmptyInk()
    {
        _anim.clip = _blinkClip;
        _anim.Play();
        _isOn = true;

        if (_originPos == null)
        {
            _originPos = new Vector2[_barRT.Length];
        }

        for (int i = 0; i < _barRT.Length; i++)
        {
            _originPos[i] = _barRT[i].anchoredPosition;
        }
    }

    void Release()
    {
        _isOn = false;
        for (int i = 0; i < _barRT.Length; i++)
        {
            _barRT[i].anchoredPosition = _originPos[i];
        }
    }

    void MoveBar()
    {
        if (!_isOn) return;

        float loopDistance = _endPos - _startPos;

        foreach (var bar in _barRT)
        {
            bar.anchoredPosition += Vector2.right * _moveSpeed * Time.deltaTime;

            if (bar.anchoredPosition.x > _endPos)
            {
                bar.anchoredPosition += Vector2.left * loopDistance;
            }
        }
    }

    public void SetWindowColor(Color color)
    {
        _frame.color = color;
        _bg.color = new Color(color.r, color.g, color.b);

        Color.RGBToHSV(color, out float h, out float s, out float v);
        foreach (var bar in _movingBar)
        {
            bar.color = Color.HSVToRGB(h, s, Mathf.Clamp01(v + 0.1f));
        }
    }

    public void CloseWnd()
    {
        //Release();
        gameObject.SetActive(false);
    }
}

using UnityEngine;

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

    bool _isOn;
    Vector2[] _originPos;


    // Update is called once per frame
    void Update()
    {
        MoveBar();
    }

    public void OpenWnd()
    {
        gameObject.SetActive(true);
        InitEmptyInk();
    }

    void InitEmptyInk()
    {
        _anim.clip = _blinkClip;
        _anim.Play();
        _isOn = true;
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

    public void MoveBar()
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

    public void CloseWnd()
    {
        Release();
        gameObject.SetActive(false);
    }
}

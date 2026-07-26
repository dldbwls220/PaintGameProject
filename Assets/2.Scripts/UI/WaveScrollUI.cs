using UnityEngine;

public class WaveScrollUI : MonoBehaviour
{
    [SerializeField] RectTransform[] _barRT;
    [SerializeField] float _moveSpeed = 100f;

    float _wrapDistance;
    float _bottomBound;

    void Start()
    {
        float spacing = Mathf.Abs(_barRT[1].anchoredPosition.y - _barRT[0].anchoredPosition.y);
        _wrapDistance = spacing * _barRT.Length;

        _bottomBound = _barRT[0].anchoredPosition.y;
        foreach (var bar in _barRT)
        {
            _bottomBound = Mathf.Min(_bottomBound, bar.anchoredPosition.y);
        }
        _bottomBound -= spacing;
    }

    void Update()
    {
        MoveBar();
    }

    void MoveBar()
    {
        foreach (var bar in _barRT)
        {
            Vector2 pos = bar.anchoredPosition;
            pos.y -= _moveSpeed * Time.deltaTime;

            if (pos.y < _bottomBound)
            {
                pos.y += _wrapDistance;
            }

            bar.anchoredPosition = pos;
        }
    }
}

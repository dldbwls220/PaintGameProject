using UnityEngine;
using UnityEngine.UI;

public class InkTankUI : MonoBehaviour
{
    [SerializeField] Slider _slider;
    [SerializeField] RectTransform _rectT;
    [SerializeField] Vector2 _screenOffset = new Vector2(180f, 250f);

    public void OpenWnd() => gameObject.SetActive(true);
    public void CloseWnd() => gameObject.SetActive(false);

    public void UpdateInkTank(float value)
    {
        _slider.value = value;
    }

    public void SetInkUIPos(Vector3 worldPos)
    {
        Camera main = Camera.main;
        if (main == null) return;

        Vector3 screenPoint = main.WorldToScreenPoint(worldPos);

        if (screenPoint.z < 0)
        {
            _rectT.gameObject.SetActive(false);
            return;
        }

        if (!_rectT.gameObject.activeSelf) _rectT.gameObject.SetActive(true);

        _rectT.position = new Vector2(screenPoint.x + _screenOffset.x, screenPoint.y + _screenOffset.y);
    }
}

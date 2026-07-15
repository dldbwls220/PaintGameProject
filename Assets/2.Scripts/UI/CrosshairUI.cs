using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    [Header("Corner Baracket")]
    [SerializeField] Image _cornerBracket;

    [Header("Crosshair")]
    [SerializeField] GameObject _crosshairObj;
    [SerializeField] Image _outerCircle;
    [SerializeField] Image _innerCircle;
    [SerializeField] Image _cross;

    Transform _targetObj;

    public void OpenCrosshair()
    {
        gameObject.SetActive(true);
    }

    public void OpenCrosshair(bool hit)
    {
        if (hit)
        {
            _outerCircle.enabled = true;
            _innerCircle.enabled = true;
        }
        else
        {
            _outerCircle.enabled = false;
            _innerCircle.enabled = false;
        }
    }

    public void DetectEnemy(bool hit)
    {
        if(hit)
            _cross.enabled = true;
        else
            _cross.enabled = false;
    }

    public void FollowTarget()
    {
        if (_targetObj == null || Camera.main == null) return;

        Vector3 screenPoint = Camera.main.WorldToScreenPoint(_targetObj.position);
        if (screenPoint.z < 0) return; // 타겟이 카메라 뒤에 있으면 무시 (반대편에 튀는 버그 방지)

        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform.parent, screenPoint, null, out Vector2 localPoint);

        ((RectTransform)transform).anchoredPosition = localPoint;
    }
}

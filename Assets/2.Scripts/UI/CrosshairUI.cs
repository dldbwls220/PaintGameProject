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
    RaycastHit _hit;

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

    public void FollowTarget(Transform targetObj)
    {
        if (targetObj == null || Camera.main == null || _crosshairObj == null) return;

        Vector3 screenPoint = Camera.main.WorldToScreenPoint(targetObj.position);
        if (screenPoint.z < 0) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, screenPoint, null, out Vector2 localPoint);

        ((RectTransform)_crosshairObj.transform).anchoredPosition = localPoint;
    }
}

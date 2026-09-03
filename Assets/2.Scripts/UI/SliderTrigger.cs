using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SliderTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] RectTransform _handler;
    Slider _slider;

    bool _dragging;

    void Awake()
    {
        _slider = GetComponent<Slider>();
        _handler.gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData e)
    {
        _dragging = true;
        _handler.gameObject.SetActive(true);
    }

    public void OnPointerUp(PointerEventData e)
    {
        _dragging = false;
        _handler.gameObject.SetActive(false);
    }

    void OnDisable() { _dragging = false; _handler.gameObject.SetActive(false); }
}

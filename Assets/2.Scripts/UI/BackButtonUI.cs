using UnityEngine;
using UnityEngine.UI;

public class BackButtonUI : MonoBehaviour
{
    [SerializeField] Image _sliderFrame;
    [SerializeField] float _fillSpeed;
    [SerializeField] float _fadeSpeed;

    public bool PressFillSlide(bool nowPress, float deltaTime)
    {
        float fillSpeed = 1 / _fillSpeed;
        float fadeSpeed = 1 / _fadeSpeed;

        if (nowPress)
        {
            _sliderFrame.fillAmount = Mathf.MoveTowards(_sliderFrame.fillAmount, 1, fillSpeed * deltaTime);

            Debug.Log("now filling");
        }
        else
        {
            _sliderFrame.fillAmount = Mathf.MoveTowards(_sliderFrame.fillAmount, 0, fadeSpeed * deltaTime);
        }

        if (_sliderFrame.fillAmount >= 1)
        {
            return true;
        }

        return false;
    }

    public void CloseWnd()
    {
        gameObject.SetActive(false);
    }
}

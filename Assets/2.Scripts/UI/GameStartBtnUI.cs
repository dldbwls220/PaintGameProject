using UnityEngine;
using UnityEngine.UI;

public class GameStartBtnUI : MonoBehaviour
{
    [SerializeField] Image _sliderFrame;
    [SerializeField] float _fillSpeed;
    [SerializeField] float _fadeSpeed;

    public bool PressFillSlide(bool nowPress)
    {
        float fillSpeed = 1 / _fillSpeed;
        float fadeSpeed = 1 / _fadeSpeed;

        if (nowPress)
        {
            _sliderFrame.fillAmount = Mathf.MoveTowards(_sliderFrame.fillAmount, 1, fillSpeed * Time.deltaTime);
        }
        else
        {
            _sliderFrame.fillAmount = Mathf.MoveTowards(_sliderFrame.fillAmount, 0, fadeSpeed * Time.deltaTime);
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

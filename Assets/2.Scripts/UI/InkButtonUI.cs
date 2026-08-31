using UnityEngine;

public class InkButtonUI : MonoBehaviour
{
    [SerializeField] Animator _inkBtnAnim;
    bool _isMouseOn;

    private void Update()
    {
        _inkBtnAnim.SetBool("IsMouseOn", _isMouseOn);
    }

    public void PointerEnter()
    {
        _isMouseOn = true;
        Debug.Log("µé¾î¿È");
    }

    public void PointerExit()
    {
        _isMouseOn = false;
    }

    public void PointerClick()
    {
        _inkBtnAnim.SetTrigger("Selected");
    }
}

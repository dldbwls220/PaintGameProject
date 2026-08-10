using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NicknameUI : MonoBehaviour
{
    [SerializeField] Image _loginBtn;
    [SerializeField] TMP_InputField _inputNickname;
    [SerializeField] TextMeshProUGUI _placeholder;

    private void Start()
    {
        _inputNickname.onSelect.AddListener(OnSelectInputField);
        _inputNickname.onDeselect.AddListener(OnDeselectInputField);
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.KeypadEnter) || Input.GetKeyUp(KeyCode.Return))
        {
            SetNickname();
        }

        CheckCurrentName();
    }

    void OnSelectInputField(string text)
    {
        _placeholder.gameObject.SetActive(false);
    }

    void OnDeselectInputField(string text)
    {
        _placeholder.gameObject.SetActive(true);
    }

    public void SetNickname()
    {
        if (_inputNickname == null) return;

        PlayerCustomizeManager.instance.SetNickname(_inputNickname.text);
        _inputNickname.text = null;
    }

    void CheckCurrentName()
    {
        _placeholder.text = PlayerCustomizeManager.instance.Data.DisplayName;
    }
}

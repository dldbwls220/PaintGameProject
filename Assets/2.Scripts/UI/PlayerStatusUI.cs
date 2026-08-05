using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusUI : MonoBehaviour
{
    [Header("Player Info Setting")]
    [SerializeField] Image _weaponIcon;
    [SerializeField] Image _cross;
    [SerializeField] Image _bg;

    [Header("Weapon Icon Grayscale")]
    [SerializeField] Material _grayscaleMaterial;
    Material _weaponIconDefaultMaterial;

    Color _color;

    void Awake()
    {
        _weaponIconDefaultMaterial = _weaponIcon.material;
    }

    public void InitPlayerInfo(Sprite weapon, bool isPlayerConnected, Color TeamColor)
    {
        if (isPlayerConnected)
        {
            if (weapon != null)
                _weaponIcon.sprite = weapon;

            _cross.enabled = !isPlayerConnected;
            _color = TeamColor;
            _bg.color = _color;
        }
        else
            PlayerDisconnected();
    }

    public void PlayerDead()
    {
        _weaponIcon.enabled = true;
        _weaponIcon.material = _grayscaleMaterial;
        _cross.enabled = true;
        _bg.color = Color.black;
    }

    public void PlayerRespawn()
    {
        _weaponIcon.enabled = true;
        _weaponIcon.material = _weaponIconDefaultMaterial;
        _cross.enabled = false;
        _bg.color = _color;
    }

    public void PlayerDisconnected()
    {
        _weaponIcon.enabled = false;
        _cross.enabled = true;
        _bg.color = Color.black;
    }
}

using UnityEngine;

public class InkTankController : MonoBehaviour
{
    [Header("Ink Setting")]
    [SerializeField] float _maxInk = 100f;
    [SerializeField] float _currentInk;
    [SerializeField] float _inktankOffset;

    public void Init()
    {
        _currentInk = _maxInk;
        _inktankOffset = 0;
    }

    public void SetInkUIPos()
    {
        if (GameUIManager._instance == null) return;

        GameUIManager._instance.SetInkUIPos(transform.position);
    }

    public void UpdateInkTank(float ink)
    {
        if(GameUIManager._instance == null) return;

        GameUIManager._instance.UpdateInkTank(ink);
    }

    public float UpdateInktankOffset()
    {
        return _inktankOffset;
    }

    public void OnOffInkTank(bool isSquid)
    {
        GameUIManager._instance.OnOffInkTank(isSquid);
    }
}

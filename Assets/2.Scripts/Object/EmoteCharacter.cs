using UnityEngine;
using UnityEngine.InputSystem;

public class EmoteCharacter : MonoBehaviour
{
    [Header("ModelObject")]
    [SerializeField] GameObject _emoteModelObj;
    SkinnedMeshRenderer[] _inklingSMR;
    [Header("Eyelids Setting")]
    [SerializeField] SkinnedMeshRenderer _eyelidsSMR;

    MaterialPropertyBlock _mpb;

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
    }

    void ApplyEyelidColor(Color color)
    {
        if (_eyelidsSMR == null) return;
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        _eyelidsSMR.GetPropertyBlock(_mpb);
        _mpb.SetColor("_BaseColor", color);
        _eyelidsSMR.SetPropertyBlock(_mpb);
    }

    void ApplyTeamColor(Color teamColor)
    {
        foreach (var s in _inklingSMR)
        {
            if (s == null) continue;

            if (s.name.Contains("_TeamC"))
            {
                s.GetPropertyBlock(_mpb);
                _mpb.SetColor("_BaseColor", teamColor);
                s.SetPropertyBlock(_mpb);
            }

            if (s.name.Contains("_TeamE"))
            {
                s.GetPropertyBlock(_mpb);
                _mpb.SetColor("_EmissionColor", teamColor);
                s.SetPropertyBlock(_mpb);
            }
        }
    }

    public void InitCharacter(Color color)
    {
        RefreshSKR();
        ApplyTeamColor(color);
    }

    public void RefreshSKR()
    {
        _inklingSMR = _emoteModelObj.GetComponentsInChildren<SkinnedMeshRenderer>(); 
    }

    public void CloseEye()
    {
        ApplyEyelidColor(new Color(1, 1, 1, 1));
    }

    public void CompletelyCloseEye()
    {
        ApplyEyelidColor(new Color(0, 0, 0, 1));
    }

    public void OpenEye()
    {
        ApplyEyelidColor(new Color(1, 1, 1, 0));
    }
}

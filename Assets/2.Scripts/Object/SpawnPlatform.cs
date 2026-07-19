using DefineEnum;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpawnPlatform : MonoBehaviour
{
    [SerializeField] SkinnedMeshRenderer _spawnPlatformSMR;
    [SerializeField] TeamState _teamState;

    MaterialPropertyBlock _mpb;

    public void InitPlatform(Color team1, Color team2)
    {
        _mpb = new MaterialPropertyBlock();

        Color teamColor = _teamState == TeamState.Team1 ? team1 : team2; 

        _spawnPlatformSMR.GetPropertyBlock(_mpb);
        _mpb.SetColor("_BaseColor", teamColor);
        _spawnPlatformSMR.SetPropertyBlock(_mpb);
    }
}

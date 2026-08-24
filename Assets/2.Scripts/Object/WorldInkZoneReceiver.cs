using UnityEngine;

/// <summary>
/// Zone 방식 잉크를 표시할 오브젝트에 붙이는 컴포넌트
/// Inspector에서 zoneIndex를 지정해 어느 Zone에 속하는지 설정
/// WorldInk/DisplayZone 셰이더를 사용하는 머티리얼 필요
/// </summary>
public class WorldInkZoneReceiver : MonoBehaviour
{
    [Tooltip("이 오브젝트가 속한 Zone 번호\nWorldInkZoneManager의 Zone 목록 순서와 일치시킬 것")]
    public int _zoneIndex = 0;

    Renderer _renderer;

    static readonly int _inkTexID   = Shader.PropertyToID("_WorldInkTex");
    static readonly int _offsetID   = Shader.PropertyToID("_ZoneOffset");
    static readonly int _sizeUID    = Shader.PropertyToID("_ZoneSizeU");
    static readonly int _sizeVID    = Shader.PropertyToID("_ZoneSizeV");
    static readonly int _axisID     = Shader.PropertyToID("_ZoneAxis");

    void Start()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer == null)
        {
            Debug.LogError($"[WorldInkZoneReceiver] Renderer가 없습니다: {gameObject.name}");
            return;
        }

        WorldInkZoneManager mgr = WorldInkZoneManager.instance;
        if (mgr == null)
        {
            Debug.LogError("[WorldInkZoneReceiver] WorldInkZoneManager가 씬에 없습니다.");
            return;
        }

        InkZone zone = mgr.GetZone(_zoneIndex);
        if (zone == null) return;

        foreach (Material mat in _renderer.materials)
        {
            if (!mat.HasProperty(_inkTexID))
            {
                Debug.LogWarning($"[WorldInkZoneReceiver] '{mat.name}'에 _WorldInkTex가 없습니다. WorldInk/DisplayZone 셰이더를 사용하세요.");
                continue;
            }

            mat.SetTexture(_inkTexID,  mgr.GetInkTexture(_zoneIndex));
            mat.SetVector (_offsetID,  zone.offset);
            mat.SetFloat  (_sizeUID,   zone.sizeU);
            mat.SetFloat  (_sizeVID,   zone.sizeV);
            mat.SetFloat  (_axisID,    (float)zone.axis);
        }
    }

    /// <summary>
    /// 이 오브젝트의 hit.point 위치 잉크 색상 반환
    /// </summary>
    public Color CheckPaintColor(RaycastHit hit)
    {
        return WorldInkZoneManager.instance.CheckPaintColor(_zoneIndex, hit.point);
    }

    public Color CheckPaintColor(Vector3 hitpos)
    {
        return WorldInkZoneManager.instance.CheckPaintColor(_zoneIndex, hitpos);
    }
}

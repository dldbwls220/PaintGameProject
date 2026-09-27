using DefineEnum;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MapZone
{
    [Tooltip("존 이름 (식별용)")]
    public string name = "Zone";

    [Tooltip("존의 월드 공간 시작 좌표")]
    public Vector2 offset = new Vector2(-50f, -50f);

    [Tooltip("존의 Death Zone")]
    public float deathHeight = -20f;

    [Tooltip("존의 가로 크기 (월드 유닛)")]
    public float sizeX = 100f;

    [Tooltip("존의 세로 크기 (월드 유닛)")]
    public float sizeY = 100f;

    [Tooltip("기즈모 색상")]
    public Color gizmoColor = Color.blue;
}

[System.Serializable]
public class MapZones
{
    [Tooltip("이 Zone 목록이 적용될 맵")]
    public MapState state;

    [Tooltip("이 맵에서 사용할 Zone 목록")]
    public List<MapZone> zones = new List<MapZone>();
}

public class MapManager : MonoBehaviour
{
    static MapManager _uniqueInstance;

    [Header("맵별 Zone 목록")]
    public List<MapZones> _zones = new List<MapZones>();

    [Tooltip("현재 활성화된 맵")]
    public MapState _activeMap = MapState.Port_Mackerel;

    [Header("Gizzmo 스위치")]
    public bool _mapSizeGizzmo;
    public bool _deathZoneGizzmo;

    public static MapManager _instance => _uniqueInstance;

    static readonly List<MapZone> _emptyZones = new List<MapZone>();

    /// <summary>
    /// 현재 활성 맵의 Zone 목록. _activeMap에 해당하는 항목이 없으면 빈 목록을 반환합니다.
    /// 에디트 모드(기즈모)와 런타임 모두에서 사용됩니다.
    /// </summary>
    List<MapZone> ActiveZones => GetMapZones(_activeMap) ?? _emptyZones;

    /// <summary>
    /// 주어진 맵의 Zone 목록을 반환합니다. 항목이 없으면 null.
    /// </summary>
    public List<MapZone> GetMapZones(MapState map)
    {
        if (_zones == null) return null;
        foreach (MapZones entry in _zones)
        {
            if (entry != null && entry.state == map)
                return entry.zones;
        }
        return null;
    }

    void Awake()
    {
        _uniqueInstance = this;

        // 전환 매니저가 선택한 맵이 있으면 그것을 우선 사용 (없으면 인스펙터 설정값 유지)
        if (WipeTransitionManager.isInstanceAlive)
            _activeMap = WipeTransitionManager.instance._mapState;

        Debug.Log($"[MapManager] 맵 \"{_activeMap}\" Zone {ActiveZones.Count}개 초기화 완료");
    }

    public void SwitchMap(MapState map)
    {
        if (GetMapZones(map) == null)
        {
            Debug.LogError($"[MapManager] \"{map}\" 맵의 Zone 목록이 _maps에 없습니다.");
            return;
        }
        if (map == _activeMap && ActiveZones.Count > 0)
            return;

        _activeMap = map;
        Debug.Log($"[MapManager] 맵 전환: \"{map}\" (Zone {ActiveZones.Count}개)");
    }

    public float GetDeathHeight()
    {
        var zones = ActiveZones;
        return zones.Count > 0 ? zones[0].deathHeight : float.NegativeInfinity;
    }

    public Vector2 XZWorldSize()
    {
        Vector2 vector = new Vector2();

        foreach (var zone in ActiveZones)
        {
            vector = new Vector2(zone.sizeX, zone.sizeY);
            break;
        }

        return vector;
    }

    void OnDrawGizmos()
    {
        foreach (var z in ActiveZones)
        {
            var c = new Vector3(z.offset.x + z.sizeX * 0.5f, 0f, z.offset.y + z.sizeY * 0.5f);
            if (_mapSizeGizzmo)
            {
                Gizmos.color = z.gizmoColor;
                Gizmos.DrawWireCube(c, new Vector3(z.sizeX, 0.1f, z.sizeY));
            }
            if (_deathZoneGizzmo)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
                Gizmos.DrawCube(new Vector3(c.x, z.deathHeight, c.z), new Vector3(z.sizeX, 0.05f, z.sizeY));
            }
        }
    }
}

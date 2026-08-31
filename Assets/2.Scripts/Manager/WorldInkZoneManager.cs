using UnityEngine;
using System.Collections.Generic;

public enum ZoneAxis { XZ, XY, ZY }

[System.Serializable]
public class InkZone
{
    [Tooltip("존 이름 (식별용)")]
    public string name = "Zone";

    [Tooltip("투영 축 선택\nXZ = 바닥/천장\nXY = 앞뒤 벽\nZY = 좌우 벽")]
    public ZoneAxis axis = ZoneAxis.XZ;

    [Tooltip("존의 월드 공간 시작 좌표")]
    public Vector2 offset = new Vector2(-50f, -50f);

    [Tooltip("존의 가로 크기 (월드 유닛)")]
    public float sizeU = 100f;

    [Tooltip("존의 세로 크기 (월드 유닛)")]
    public float sizeV = 100f;

    [Tooltip("잉크 텍스처 해상도")]
    public int textureSize = 2048;

    [Tooltip("기즈모 색상")]
    public Color gizmoColor = Color.blue;

    [Header("높이 필터 (XZ 바닥 전용)")]
    [Tooltip("켜면 Y 범위 안에 있는 hit만 이 Zone에 자동 매핑됩니다.")]
    public bool useHeightFilter = false;
    public float heightMin = 0f;
    public float heightMax = 5f;

    [Header("깊이 필터 (XY/ZY 벽 전용)")]
    [Tooltip("XY 축: Z 범위로 남북 벽 구분 / ZY 축: X 범위로 동서 벽 구분")]
    public bool useDepthFilter = false;
    [Tooltip("XY 축: 최소 Z / ZY 축: 최소 X")]
    public float depthMin = 0f;
    [Tooltip("XY 축: 최대 Z / ZY 축: 최대 X")]
    public float depthMax = 10f;

    [System.NonSerialized]
    public RenderTexture inkTexture;
}

public class WorldInkZoneManager : MonoBehaviour
{
    static WorldInkZoneManager _uniqueinstance;

    [Header("Zone 목록")]
    public List<InkZone> _zones = new List<InkZone>();

    [Header("셰이더")]
    public Shader _brushShader;

    [Header("Gizzmo 스위치")]
    public bool _offXZGizzmo;
    public bool _offXYGizzmo;
    public bool _offZYGizzmo;

    Material _brushMaterial;

    static readonly int _brushPosID      = Shader.PropertyToID("_BrushPos");
    static readonly int _brushRadiusID   = Shader.PropertyToID("_BrushRadius");
    static readonly int _brushColorID    = Shader.PropertyToID("_BrushColor");
    static readonly int _brushHardnessID = Shader.PropertyToID("_BrushHardness");
    static readonly int _aspectRatioID   = Shader.PropertyToID("_AspectRatio");

    public static WorldInkZoneManager _instance => _uniqueinstance;

    

    void Awake()
    {
        _uniqueinstance = this;

        _brushMaterial = new Material(_brushShader);
        foreach (InkZone zone in _zones)
        {
            zone.inkTexture            = new RenderTexture(zone.textureSize, zone.textureSize, 0, RenderTextureFormat.ARGB32);
            zone.inkTexture.filterMode = FilterMode.Bilinear;
            zone.inkTexture.wrapMode   = TextureWrapMode.Clamp;
            zone.inkTexture.Create();
        }
        Debug.Log($"[WorldInkZoneManager] Zone {_zones.Count}개 초기화 완료");
    }

    public void Paint(int zoneIndex, Vector3 hitPoint, Color inkColor, float radius = 1f, float hardness = 0.8f)
    {
        if (!IsValidIndex(zoneIndex)) return;
        InkZone zone = _zones[zoneIndex];
        float u, v;
        GetUV(zone, hitPoint, out u, out v);
        if (u < 0f || u > 1f || v < 0f || v > 1f)
        {
            Debug.LogWarning($"[WorldInkZoneManager] Zone[{zoneIndex}] 범위 밖: {hitPoint}");
            return;
        }
        float uvRadius    = radius / zone.sizeU;
        float aspectRatio = zone.sizeV / zone.sizeU;
        _brushMaterial.SetVector(_brushPosID,      new Vector4(u, v, 0f, 0f));
        _brushMaterial.SetFloat (_brushRadiusID,   uvRadius);
        _brushMaterial.SetColor (_brushColorID,    inkColor);
        _brushMaterial.SetFloat (_brushHardnessID, hardness);
        _brushMaterial.SetFloat (_aspectRatioID,   aspectRatio);
        RenderTexture temp = RenderTexture.GetTemporary(zone.inkTexture.descriptor);
        Graphics.Blit(zone.inkTexture, temp, _brushMaterial);
        Graphics.Blit(temp, zone.inkTexture);
        RenderTexture.ReleaseTemporary(temp);
    }

    /// <summary>
    /// hit 지점의 오브젝트에서 WorldInkZoneReceiver를 찾아 해당 Zone에 칠합니다.
    /// Receiver가 없으면 FindZone으로 폴백합니다.
    /// </summary>
    public void PaintAuto(Vector3 hitPoint, Vector3 hitNormal, Color inkColor, float radius = 1f, float hardness = 0.8f)
    {
        // hit 지점 주변 오브젝트에서 WorldInkZoneReceiver 검색
        Collider[] cols = Physics.OverlapSphere(hitPoint, 0.05f);
        foreach (Collider col in cols)
        {
            WorldInkZoneReceiver receiver = col.GetComponentInParent<WorldInkZoneReceiver>();
            if (receiver != null)
            {
                Paint(receiver._zoneIndex, hitPoint, inkColor, radius, hardness);
                return;
            }
        }

        // Receiver가 없으면 FindZone으로 폴백
        int zoneIndex = FindZone(hitPoint, hitNormal);
        if (zoneIndex < 0)
        {
            Debug.LogWarning($"[WorldInkZoneManager] hit.point={hitPoint}에 맞는 Zone을 찾지 못했습니다.");
            return;
        }
        Paint(zoneIndex, hitPoint, inkColor, radius, hardness);
    }

    public int FindZone(Vector3 hitPoint, Vector3 hitNormal)
    {
        Vector3 absN = new Vector3(Mathf.Abs(hitNormal.x), Mathf.Abs(hitNormal.y), Mathf.Abs(hitNormal.z));
        ZoneAxis targetAxis;
        if (absN.y >= absN.x && absN.y >= absN.z)       targetAxis = ZoneAxis.XZ;
        else if (absN.x >= absN.z)                       targetAxis = ZoneAxis.ZY;
        else                                             targetAxis = ZoneAxis.XY;

        // 필터 켜진 Zone 우선
        for (int i = 0; i < _zones.Count; i++)
        {
            InkZone z = _zones[i];
            if (z.axis != targetAxis) continue;
            if (targetAxis == ZoneAxis.XZ)
            {
                if (!z.useHeightFilter) continue;
                if (hitPoint.y < z.heightMin || hitPoint.y > z.heightMax) continue;
            }
            else if (targetAxis == ZoneAxis.XY)
            {
                if (!z.useDepthFilter) continue;
                if (hitPoint.z < z.depthMin || hitPoint.z > z.depthMax) continue;
            }
            else
            {
                if (!z.useDepthFilter) continue;
                if (hitPoint.x < z.depthMin || hitPoint.x > z.depthMax) continue;
            }
            float u, v;
            GetUV(z, hitPoint, out u, out v);
            if (u >= 0f && u <= 1f && v >= 0f && v <= 1f) return i;
        }

        // 필터 없는 Zone 폴백
        for (int i = 0; i < _zones.Count; i++)
        {
            InkZone z = _zones[i];
            if (z.axis != targetAxis) continue;
            if (z.useHeightFilter || z.useDepthFilter) continue;
            float u, v;
            GetUV(z, hitPoint, out u, out v);
            if (u >= 0f && u <= 1f && v >= 0f && v <= 1f) return i;
        }
        return -1;
    }

    void GetUV(InkZone zone, Vector3 hitPoint, out float u, out float v)
    {
        switch (zone.axis)
        {
            case ZoneAxis.XZ:
                u = (hitPoint.x - zone.offset.x) / zone.sizeU;
                v = (hitPoint.z - zone.offset.y) / zone.sizeV;
                break;
            case ZoneAxis.XY:
                u = (hitPoint.x - zone.offset.x) / zone.sizeU;
                v = (hitPoint.y - zone.offset.y) / zone.sizeV;
                break;
            case ZoneAxis.ZY:
                u = (hitPoint.z - zone.offset.x) / zone.sizeU;
                v = (hitPoint.y - zone.offset.y) / zone.sizeV;
                break;
            default:
                u = v = 0f;
                break;
        }
    }

    public RenderTexture GetInkTexture(int zoneIndex)
    {
        if (!IsValidIndex(zoneIndex)) return null;
        return _zones[zoneIndex].inkTexture;
    }

    public InkZone GetZone(int zoneIndex)
    {
        if (!IsValidIndex(zoneIndex)) return null;
        return _zones[zoneIndex];
    }

    public Color CheckPaintColor(int zoneIndex, Vector3 hitPoint)
    {
        if (!IsValidIndex(zoneIndex)) return Color.clear;
        InkZone zone = _zones[zoneIndex];
        float u, v;
        GetUV(zone, hitPoint, out u, out v);
        if (u < 0f || u > 1f || v < 0f || v > 1f) return Color.clear;
        int px = Mathf.Clamp(Mathf.FloorToInt(u * zone.inkTexture.width),  0, zone.inkTexture.width  - 1);
        int py = Mathf.Clamp(Mathf.FloorToInt(v * zone.inkTexture.height), 0, zone.inkTexture.height - 1);
        Texture2D tempTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        RenderTexture prev   = RenderTexture.active;
        RenderTexture.active = zone.inkTexture;
        tempTex.ReadPixels(new Rect(px, py, 1, 1), 0, 0);
        tempTex.Apply();
        RenderTexture.active = prev;
        Color result = tempTex.GetPixel(0, 0);
        Destroy(tempTex);
        return result;
    }

    public Vector2 XZWorldSize()
    {
        Vector2 vector = new Vector2();

        foreach (var zone in _zones)
        {
            if (zone.axis == ZoneAxis.XZ)
            {
                vector = new Vector2(zone.sizeU, zone.sizeV);
                break;
            }
        }

        return vector;
    }

    bool IsValidIndex(int index)
    {
        if (index < 0 || index >= _zones.Count)
        {
            Debug.LogError($"[WorldInkZoneManager] 잘못된 Zone 번호: {index} (총 {_zones.Count}개)");
            return false;
        }
        return true;
    }

    void OnDrawGizmos()
    {
        if (_zones == null) return;
        foreach (InkZone zone in _zones)
        {
            if(!_offXYGizzmo && zone.axis == ZoneAxis.XY) { continue; }
            if (!_offXZGizzmo && zone.axis == ZoneAxis.XZ) { continue; }
            if (!_offZYGizzmo && zone.axis == ZoneAxis.ZY) { continue; }

            Vector3 center, size;
            switch (zone.axis)
            {
                case ZoneAxis.XZ:
                    float gizmoY = zone.useHeightFilter ? (zone.heightMin + zone.heightMax) * 0.5f : 0f;
                    float gizmoH = zone.useHeightFilter ? (zone.heightMax - zone.heightMin)        : 0.1f;
                    center = new Vector3(zone.offset.x + zone.sizeU * 0.5f, gizmoY, zone.offset.y + zone.sizeV * 0.5f);
                    size   = new Vector3(zone.sizeU, gizmoH, zone.sizeV);
                    break;
                case ZoneAxis.XY:
                    float gizmoZC = zone.useDepthFilter ? (zone.depthMin + zone.depthMax) * 0.5f : 0f;
                    float gizmoZS = zone.useDepthFilter ? (zone.depthMax - zone.depthMin)        : 0.1f;
                    center = new Vector3(zone.offset.x + zone.sizeU * 0.5f, zone.offset.y + zone.sizeV * 0.5f, gizmoZC);
                    size   = new Vector3(zone.sizeU, zone.sizeV, gizmoZS);
                    break;
                case ZoneAxis.ZY:
                    float gizmoXC = zone.useDepthFilter ? (zone.depthMin + zone.depthMax) * 0.5f : 0f;
                    float gizmoXS = zone.useDepthFilter ? (zone.depthMax - zone.depthMin)        : 0.1f;
                    center = new Vector3(gizmoXC, zone.offset.y + zone.sizeV * 0.5f, zone.offset.x + zone.sizeU * 0.5f);
                    size   = new Vector3(gizmoXS, zone.sizeV, zone.sizeU);
                    break;
                default:
                    continue;
            }
            Gizmos.color = new Color(zone.gizmoColor.r, zone.gizmoColor.g, zone.gizmoColor.b, 0.15f);
            Gizmos.DrawCube(center, size);
            Gizmos.color = zone.gizmoColor;
            Gizmos.DrawWireCube(center, size);
        }
    }

    void OnDisable()
    {
        foreach (InkZone zone in _zones)
        {
            if (zone.inkTexture != null)
                zone.inkTexture.Release();
        }
    }
}


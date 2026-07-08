using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 투영 축 종류
/// XZ : 바닥/천장  (수평면, Y 고정)
/// XY : 앞뒤 벽   (Z 고정)
/// ZY : 좌우 벽   (X 고정)
/// </summary>
public enum ZoneAxis { XZ, XY, ZY }

/// <summary>
/// 잉크 존 하나를 정의하는 데이터
/// Inspector에서 직접 설정
/// </summary>
[System.Serializable]
public class InkZone
{
    [Tooltip("존 이름 (식별용)")]
    public string name = "Zone";

    [Tooltip("투영 축 선택\nXZ = 바닥/천장\nXY = 앞뒤 벽\nZY = 좌우 벽")]
    public ZoneAxis axis = ZoneAxis.XZ;

    [Tooltip("존의 월드 공간 시작 좌표\nXZ: (X시작, Z시작)\nXY: (X시작, Y시작)\nZY: (Z시작, Y시작)")]
    public Vector2 offset = new Vector2(-50f, -50f);

    [Tooltip("존의 가로 크기 (월드 유닛)")]
    public float sizeU = 100f;

    [Tooltip("존의 세로 크기 (월드 유닛)")]
    public float sizeV = 100f;

    [Tooltip("잉크 텍스처 해상도")]
    public int textureSize = 2048;

    [Tooltip("기즈모 색상 (Scene 뷰 확인용)")]
    public Color gizmoColor = Color.blue;

    // 런타임에 생성되는 RenderTexture (Inspector 노출 안 함)
    [System.NonSerialized]
    public RenderTexture inkTexture;
}

/// <summary>
/// 여러 Zone을 관리하는 매니저
/// Zone마다 별도 RenderTexture를 가져 겹침 문제 해결
/// </summary>
public class WorldInkZoneManager : Singleton<WorldInkZoneManager>
{
    [Header("Zone 목록")]
    public List<InkZone> _zones = new List<InkZone>();

    [Header("셰이더")]
    public Shader _brushShader;

    Material _brushMaterial;

    static readonly int _brushPosID      = Shader.PropertyToID("_BrushPos");
    static readonly int _brushRadiusID   = Shader.PropertyToID("_BrushRadius");
    static readonly int _brushColorID    = Shader.PropertyToID("_BrushColor");
    static readonly int _brushHardnessID = Shader.PropertyToID("_BrushHardness");

    public override void Awake()
    {
        base.Awake();

        _brushMaterial = new Material(_brushShader);

        // 모든 Zone의 RenderTexture 생성
        foreach (InkZone zone in _zones)
        {
            zone.inkTexture            = new RenderTexture(zone.textureSize, zone.textureSize, 0, RenderTextureFormat.ARGB32);
            zone.inkTexture.filterMode = FilterMode.Bilinear;
            zone.inkTexture.wrapMode   = TextureWrapMode.Clamp;
            zone.inkTexture.Create();
        }

        Debug.Log($"[WorldInkZoneManager] Zone {_zones.Count}개 초기화 완료");
    }

    /// <summary>
    /// Zone 번호를 직접 지정해서 페인트
    /// WorldInkZoneReceiver에서 호출
    /// </summary>
    public void Paint(int zoneIndex, Vector3 hitPoint, Color inkColor, float radius = 1f, float hardness = 0.8f)
    {
        if (!IsValidIndex(zoneIndex)) return;

        InkZone zone = _zones[zoneIndex];

        // 존의 투영 축에 따라 UV 계산
        float u, v;
        GetUV(zone, hitPoint, out u, out v);

        // 존 범위 밖이면 무시
        if (u < 0f || u > 1f || v < 0f || v > 1f)
        {
            Debug.LogWarning($"[WorldInkZoneManager] Zone[{zoneIndex}] 범위 밖: {hitPoint}");
            return;
        }

        // 브러시 반경은 U 크기 기준으로 변환 (sizeU 기준)
        float uvRadius = radius / zone.sizeU;

        _brushMaterial.SetVector(_brushPosID,      new Vector4(u, v, 0f, 0f));
        _brushMaterial.SetFloat (_brushRadiusID,   uvRadius);
        _brushMaterial.SetColor (_brushColorID,    inkColor);
        _brushMaterial.SetFloat (_brushHardnessID, hardness);

        RenderTexture temp = RenderTexture.GetTemporary(zone.inkTexture.descriptor);
        Graphics.Blit(zone.inkTexture, temp, _brushMaterial);
        Graphics.Blit(temp, zone.inkTexture);
        RenderTexture.ReleaseTemporary(temp);
    }

    /// <summary>
    /// Zone의 투영 축에 따라 hit.point를 UV로 변환
    /// </summary>
    void GetUV(InkZone zone, Vector3 hitPoint, out float u, out float v)
    {
        switch (zone.axis)
        {
            case ZoneAxis.XZ:
                // 바닥/천장: X → U, Z → V
                u = (hitPoint.x - zone.offset.x) / zone.sizeU;
                v = (hitPoint.z - zone.offset.y) / zone.sizeV;
                break;

            case ZoneAxis.XY:
                // 앞뒤 벽: X → U, Y → V
                u = (hitPoint.x - zone.offset.x) / zone.sizeU;
                v = (hitPoint.y - zone.offset.y) / zone.sizeV;
                break;

            case ZoneAxis.ZY:
                // 좌우 벽: Z → U, Y → V
                u = (hitPoint.z - zone.offset.x) / zone.sizeU;
                v = (hitPoint.y - zone.offset.y) / zone.sizeV;
                break;

            default:
                u = v = 0f;
                break;
        }
    }

    /// <summary>
    /// Zone의 RenderTexture 반환 (WorldInkZoneReceiver에서 사용)
    /// </summary>
    public RenderTexture GetInkTexture(int zoneIndex)
    {
        if (!IsValidIndex(zoneIndex)) return null;
        return _zones[zoneIndex].inkTexture;
    }

    /// <summary>
    /// Zone의 설정값 반환 (셰이더 파라미터 전달용)
    /// </summary>
    public InkZone GetZone(int zoneIndex)
    {
        if (!IsValidIndex(zoneIndex)) return null;
        return _zones[zoneIndex];
    }

    /// <summary>
    /// hit.point의 색상을 읽습니다
    /// </summary>
    public Color CheckPaintColor(int zoneIndex, Vector3 hitPoint)
    {
        if (!IsValidIndex(zoneIndex)) return Color.clear;

        InkZone zone = _zones[zoneIndex];
        float u, v;
        GetUV(zone, hitPoint, out u, out v);

        if (u < 0f || u > 1f || v < 0f || v > 1f)
            return Color.clear;

        int px = Mathf.Clamp(Mathf.FloorToInt(u * zone.inkTexture.width),  0, zone.inkTexture.width  - 1);
        int py = Mathf.Clamp(Mathf.FloorToInt(v * zone.inkTexture.height), 0, zone.inkTexture.height - 1);

        Texture2D tempTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        RenderTexture prev    = RenderTexture.active;
        RenderTexture.active  = zone.inkTexture;

        tempTex.ReadPixels(new Rect(px, py, 1, 1), 0, 0);
        tempTex.Apply();

        RenderTexture.active = prev;
        Color result = tempTex.GetPixel(0, 0);
        Destroy(tempTex);

        return result;
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
            Vector3 center, size;

            switch (zone.axis)
            {
                case ZoneAxis.XZ:
                    center = new Vector3(zone.offset.x + zone.sizeU * 0.5f, 0f, zone.offset.y + zone.sizeV * 0.5f);
                    size   = new Vector3(zone.sizeU, 0f, zone.sizeV);
                    break;
                case ZoneAxis.XY:
                    center = new Vector3(zone.offset.x + zone.sizeU * 0.5f, zone.offset.y + zone.sizeV * 0.5f, 0f);
                    size   = new Vector3(zone.sizeU, zone.sizeV, 0f);
                    break;
                case ZoneAxis.ZY:
                    center = new Vector3(0f, zone.offset.y + zone.sizeV * 0.5f, zone.offset.x + zone.sizeU * 0.5f);
                    size   = new Vector3(0f, zone.sizeV, zone.sizeU);
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

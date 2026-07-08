using UnityEngine;

/// <summary>
/// Triplanar 잉크를 표시할 오브젝트에 붙이는 컴포넌트
/// WorldInkManagerTriplanar의 3개 텍스처를 머티리얼에 연결
/// WorldInk/DisplayTriplanar 셰이더를 사용하는 머티리얼 필요
/// </summary>
public class WorldInkReceiverTriplanar : MonoBehaviour
{
    Renderer _renderer;

    static readonly int _inkTexXZID   = Shader.PropertyToID("_WorldInkTexXZ");
    static readonly int _inkTexXYID   = Shader.PropertyToID("_WorldInkTexXY");
    static readonly int _inkTexZYID   = Shader.PropertyToID("_WorldInkTexZY");
    static readonly int _mapOffsetID  = Shader.PropertyToID("_WorldMapOffset");
    static readonly int _mapSizeID    = Shader.PropertyToID("_WorldMapSize");
    static readonly int _mapMinYID    = Shader.PropertyToID("_WorldMapMinY");
    static readonly int _mapHeightID  = Shader.PropertyToID("_WorldMapHeight");

    void Start()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer == null)
        {
            Debug.LogError($"[WorldInkReceiverTriplanar] Renderer가 없습니다: {gameObject.name}");
            return;
        }

        WorldInkManagerTriplanar mgr = WorldInkManagerTriplanar.instance;
        if (mgr == null)
        {
            Debug.LogError("[WorldInkReceiverTriplanar] WorldInkManagerTriplanar가 씬에 없습니다.");
            return;
        }

        foreach (Material mat in _renderer.materials)
        {
            if (!mat.HasProperty(_inkTexXZID))
            {
                Debug.LogWarning($"[WorldInkReceiverTriplanar] '{mat.name}'에 _WorldInkTexXZ가 없습니다. WorldInk/DisplayTriplanar 셰이더를 사용하세요.");
                continue;
            }

            // 3방향 잉크 텍스처 연결
            mat.SetTexture(_inkTexXZID,  mgr.InkTexXZ);
            mat.SetTexture(_inkTexXYID,  mgr.InkTexXY);
            mat.SetTexture(_inkTexZYID,  mgr.InkTexZY);

            // 맵 범위 정보 전달
            mat.SetVector(_mapOffsetID,  mgr.MapOffset);
            mat.SetFloat (_mapSizeID,    mgr.MapSize);
            mat.SetFloat (_mapMinYID,    mgr.MapMinY);
            mat.SetFloat (_mapHeightID,  mgr.MapHeight);
        }
    }

    /// <summary>
    /// 특정 hit 위치의 잉크 색상을 읽습니다.
    /// 법선 방향의 가중치가 가장 높은 텍스처에서 읽습니다.
    /// </summary>
    public Color CheckPaintColor(RaycastHit hit)
    {
        WorldInkManagerTriplanar mgr = WorldInkManagerTriplanar.instance;

        Vector3 absNormal = new Vector3(
            Mathf.Abs(hit.normal.x),
            Mathf.Abs(hit.normal.y),
            Mathf.Abs(hit.normal.z)
        );

        // 법선 가중치가 가장 큰 방향의 텍스처에서 읽기
        RenderTexture targetTex;
        float u, v;

        if (absNormal.y >= absNormal.x && absNormal.y >= absNormal.z)
        {
            // 바닥/천장 → XZ 텍스처
            targetTex = mgr.InkTexXZ;
            u = (hit.point.x - mgr.MapOffset.x) / mgr.MapSize;
            v = (hit.point.z - mgr.MapOffset.y) / mgr.MapSize;
        }
        else if (absNormal.x >= absNormal.z)
        {
            // 좌우 벽 → ZY 텍스처
            targetTex = mgr.InkTexZY;
            u = (hit.point.z - mgr.MapOffset.y) / mgr.MapSize;
            v = (hit.point.y - mgr.MapMinY)      / mgr.MapHeight;
        }
        else
        {
            // 앞뒤 벽 → XY 텍스처
            targetTex = mgr.InkTexXY;
            u = (hit.point.x - mgr.MapOffset.x) / mgr.MapSize;
            v = (hit.point.y - mgr.MapMinY)      / mgr.MapHeight;
        }

        if (u < 0f || u > 1f || v < 0f || v > 1f)
            return Color.clear;

        int px = Mathf.Clamp(Mathf.FloorToInt(u * targetTex.width),  0, targetTex.width  - 1);
        int py = Mathf.Clamp(Mathf.FloorToInt(v * targetTex.height), 0, targetTex.height - 1);

        Texture2D tempTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = targetTex;

        tempTex.ReadPixels(new Rect(px, py, 1, 1), 0, 0);
        tempTex.Apply();

        RenderTexture.active = prev;
        Color result = tempTex.GetPixel(0, 0);

        Destroy(tempTex);
        return result;
    }
}

using UnityEngine;

/// <summary>
/// 잉크를 표시할 지형 오브젝트에 붙이는 컴포넌트
/// WorldInkManager의 잉크 텍스처를 머티리얼에 연결해줌
/// WorldInkDisplay 셰이더를 사용하는 머티리얼이 필요함
/// </summary>
public class WorldInkReceiver : MonoBehaviour
{
    Renderer _renderer;

    static readonly int _inkTexID    = Shader.PropertyToID("_WorldInkTex");
    static readonly int _mapOffsetID = Shader.PropertyToID("_WorldMapOffset");
    static readonly int _mapSizeID   = Shader.PropertyToID("_WorldMapSize");

    void Start()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer == null)
        {
            Debug.LogError($"[WorldInkReceiver] Renderer가 없습니다: {gameObject.name}");
            return;
        }

        if (WorldInkManager.instance == null)
        {
            Debug.LogError("[WorldInkReceiver] WorldInkManager가 씬에 없습니다.");
            return;
        }

        // 머티리얼 인스턴스에 잉크 텍스처와 맵 정보 전달
        // renderer.materials를 쓰면 자동으로 인스턴스 머티리얼이 생성되어
        // 다른 오브젝트와 머티리얼을 공유해도 독립적으로 적용됨
        foreach (Material mat in _renderer.materials)
        {
            if (!mat.HasProperty(_inkTexID))
            {
                Debug.LogWarning($"[WorldInkReceiver] '{mat.name}'에 _WorldInkTex 프로퍼티가 없습니다. WorldInk/Display 셰이더를 사용하세요.");
                continue;
            }

            // WorldInkManager의 전역 잉크 텍스처 연결
            mat.SetTexture(_inkTexID,    WorldInkManager.instance.InkTexture);

            // 맵 범위 정보 전달 (셰이더에서 XZ → UV 변환에 사용)
            mat.SetVector(_mapOffsetID,  WorldInkManager.instance.MapOffset);
            mat.SetFloat (_mapSizeID,    WorldInkManager.instance.MapSize);
        }
    }

    public Color CheckPaintColor(RaycastHit hit)
    {
        WorldInkManager mgr = WorldInkManager.instance;

        // 월드 XZ → 잉크 텍스처 UV 변환
        float u = (hit.point.x - mgr.MapOffset.x) / mgr.MapSize;
        float v = (hit.point.z - mgr.MapOffset.y) / mgr.MapSize;

        // 맵 범위 밖이면 빈 색상 반환
        if (u < 0f || u > 1f || v < 0f || v > 1f)
            return Color.clear;

        int textureSize = mgr.InkTexture.width;
        int px = Mathf.Clamp(Mathf.FloorToInt(u * textureSize), 0, textureSize - 1);
        int py = Mathf.Clamp(Mathf.FloorToInt(v * textureSize), 0, textureSize - 1);

        Texture2D tempTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = mgr.InkTexture;

        tempTex.ReadPixels(new Rect(px, py, 1, 1), 0, 0);
        tempTex.Apply();

        RenderTexture.active = prev;
        Color detectedColor = tempTex.GetPixel(0, 0);

        Destroy(tempTex);
        return detectedColor;
    }
}

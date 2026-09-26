using UnityEngine;

public class Paintabale : MonoBehaviour
{
    const int TEXTURE_SIZE = 1024;

    public float _extendsIslandOffset = 1;

    RenderTexture _extendIslandsRenderTexture;
    RenderTexture _uvIslandsRenderTexture;
    RenderTexture _maskRenderTexture;
    RenderTexture _supportTexture;

    Renderer _renderer;

    int _maskTextureID = Shader.PropertyToID("_MaskTexture");

    public RenderTexture getmask() => _maskRenderTexture;
    public RenderTexture getUVIslands() => _uvIslandsRenderTexture;
    public RenderTexture getExtend() => _extendIslandsRenderTexture;
    public RenderTexture getSupport() => _supportTexture;
    public Renderer getRenderer() => _renderer;

    void Start()
    {
        _maskRenderTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0);
        _maskRenderTexture.filterMode = FilterMode.Bilinear;

        _extendIslandsRenderTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0);
        _extendIslandsRenderTexture.filterMode = FilterMode.Bilinear;

        _uvIslandsRenderTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0);
        _uvIslandsRenderTexture.filterMode = FilterMode.Bilinear;

        _supportTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0);
        _supportTexture.filterMode = FilterMode.Bilinear;

        _renderer = GetComponent<Renderer>();

        if (_renderer == null)
        {
            Debug.LogError($"[Paintabale] Renderer가 없습니다: {gameObject.name}");
            return;
        }

        // 다중 머티리얼 슬롯 모두에 _MaskTexture 적용
        bool hasPaintable = false;
        foreach (Material mat in _renderer.materials)
        {
            if (mat.HasProperty(_maskTextureID))
            {
                mat.SetTexture(_maskTextureID, _extendIslandsRenderTexture);
                hasPaintable = true;
            }
        }

        if (!hasPaintable)
        {
            Debug.LogError($"[Paintabale] '{gameObject.name}'의 머티리얼 중 _MaskTexture 프로퍼티를 가진 것이 없습니다. M_Paintable 셰이더 머티리얼이 필요합니다.");
            return;
        }

        PaintManager.instance.initTextures(this);
    }

    public Color CheckPaintColor(RaycastHit hit)
    {
        if (hit.collider.gameObject == this.gameObject)
        {
            Vector2 uv = hit.textureCoord;

            // 타일링으로 [0,1] 범위를 벗어날 수 있으므로 Repeat으로 정규화
            uv.x = Mathf.Repeat(uv.x, 1f);
            uv.y = Mathf.Repeat(uv.y, 1f);

            int px = Mathf.Clamp(Mathf.FloorToInt(uv.x * TEXTURE_SIZE), 0, TEXTURE_SIZE - 1);
            int py = Mathf.Clamp(Mathf.FloorToInt(uv.y * TEXTURE_SIZE), 0, TEXTURE_SIZE - 1);

            Texture2D tempTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = _extendIslandsRenderTexture;

            tempTex.ReadPixels(new Rect(px, py, 1, 1), 0, 0);
            tempTex.Apply();

            RenderTexture.active = prev;
            Color detectedColor = tempTex.GetPixel(0, 0);

            Destroy(tempTex);
            return detectedColor;
        }
        return Color.clear;
    }

    void OnDisable()
    {
        _maskRenderTexture.Release();
        _uvIslandsRenderTexture.Release();
        _extendIslandsRenderTexture.Release();
        _supportTexture.Release();
    }
}

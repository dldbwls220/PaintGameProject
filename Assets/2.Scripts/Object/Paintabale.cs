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
        _renderer.material.SetTexture(_maskTextureID, _extendIslandsRenderTexture);

        PaintManager.instance.initTextures(this);
    }

    public Color CheckPaintColor(RaycastHit hit)
    {
        if (hit.collider.gameObject == this.gameObject)
        {
            Vector2 uv = hit.textureCoord;
            // 2. RenderTexture에서 해당 UV의 픽셀 읽기
            Texture2D tempTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = _extendIslandsRenderTexture;

            tempTex.ReadPixels(new Rect(uv.x * TEXTURE_SIZE, uv.y * TEXTURE_SIZE, 1, 1), 0, 0);
            tempTex.Apply();

            RenderTexture.active = prev;
            Color detectedColor = tempTex.GetPixel(0, 0);

            Destroy(tempTex); // 메모리 누수 방지
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

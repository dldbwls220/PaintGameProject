using UnityEngine;

public class Paintabale : MonoBehaviour
{

    public float _extendsIslandOffset = 1;

    [Header("Texture Size")]
    [SerializeField] int TEXTURE_SIZE;
    [SerializeField] float _texelsPerMeter = 64f;   // 1m당 텍셀 수 (게임 전체 일관성 기준)
    [SerializeField] int _minSize = 128, _maxSize = 2048;
    [SerializeField] int _overrideSize = 0;         // 0이면 자동, 아니면 수동 지정

    [Header("Bump Noise")]
    [SerializeField] float _bumpNoisePerMeter = 0.5f;
    [SerializeField] float _bumpNoiseOverrider = 0;
    [SerializeField] int _bumpNoiseUVChanel = 0;
    [SerializeField] float _bumpScale;

    RenderTexture _extendIslandsRenderTexture;
    RenderTexture _uvIslandsRenderTexture;
    RenderTexture _maskRenderTexture;
    RenderTexture _supportTexture;

    Renderer _renderer;

    static readonly int _bumpNoiseScaleID = Shader.PropertyToID("Vector1_b5cc7f6f25194a778cb438f45fbbce66");
    int _maskTextureID = Shader.PropertyToID("_MaskTexture");

    public RenderTexture getmask() => _maskRenderTexture;
    public RenderTexture getUVIslands() => _uvIslandsRenderTexture;
    public RenderTexture getExtend() => _extendIslandsRenderTexture;
    public RenderTexture getSupport() => _supportTexture;
    public Renderer getRenderer() => _renderer;

    void Start()
    {
        TEXTURE_SIZE = CalcTextureSize();

        _maskRenderTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0);
        _maskRenderTexture.filterMode = FilterMode.Bilinear;

        _extendIslandsRenderTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0);
        _extendIslandsRenderTexture.filterMode = FilterMode.Bilinear;

        _uvIslandsRenderTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0);
        _uvIslandsRenderTexture.filterMode = FilterMode.Bilinear;

        _supportTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0);
        _supportTexture.filterMode = FilterMode.Bilinear;

        // 새로 만든 RenderTexture는 내용이 보장되지 않아(이전에 해제된 다른 오브젝트의
        // 텍스처 잔상 등 GPU 메모리 쓰레기가 남아있을 수 있음), 실제로는 칠해진 적이 없는데도
        // 잉크가 묻어있는 것처럼 보일 수 있다. 페인팅 전에 반드시 투명하게 비워둔다.
        ClearRenderTexture(_maskRenderTexture);
        ClearRenderTexture(_extendIslandsRenderTexture);
        ClearRenderTexture(_supportTexture);

        _renderer = GetComponent<Renderer>();

        if (_renderer == null)
        {
            Debug.LogError($"[Paintabale] Renderer가 없습니다: {gameObject.name}");
            return;
        }

        // 다중 머티리얼 슬롯 모두에 _MaskTexture / _BumpNoiseScale 적용
        float bumpScale = CalcBumpNoiseScale();
        bool hasPaintable = false;
        foreach (Material mat in _renderer.materials)
        {
            if (mat.HasProperty(_maskTextureID))
            {
                mat.SetTexture(_maskTextureID, _extendIslandsRenderTexture);
                hasPaintable = true;
            }

            if (mat.HasProperty(_bumpNoiseScaleID))
                mat.SetFloat(_bumpNoiseScaleID, bumpScale);
        }

        if (!hasPaintable)
        {
            Debug.LogError($"[Paintabale] '{gameObject.name}'의 머티리얼 중 _MaskTexture 프로퍼티를 가진 것이 없습니다. M_Paintable 셰이더 머티리얼이 필요합니다.");
            return;
        }

        PaintManager.instance.initTextures(this);
    }

    // 페인트 셰이더(TexturePainter)가 사용하는 UV 채널 (TEXCOORD1)
    const int PAINT_UV_CHANNEL = 1;

    int CalcTextureSize()
    {
        if (_overrideSize > 0) return _overrideSize;

        // 맵 메시는 머티리얼별로 합쳐져 있어 bounds가 맵 전체 크기가 되므로 쓸 수 없다.
        // 대신 UV 면적 1당 실제 표면 면적(메시 단위²)을 이용해 텍셀 밀도를 맞춘다.
        MeshFilter mf = GetComponent<MeshFilter>();
        float metric = mf != null && mf.sharedMesh != null ? mf.sharedMesh.GetUVDistributionMetric(PAINT_UV_CHANNEL) : 0f;

        if (metric <= 0f || float.IsInfinity(metric) || float.IsNaN(metric))
        {
            Debug.LogWarning($"[Paintabale] '{gameObject.name}' UV 분포 값을 얻지 못해 기본 크기 1024를 사용합니다.");
            return Mathf.Clamp(1024, _minSize, _maxSize);
        }

        Vector3 ls = transform.lossyScale;
        float scale = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));

        // UV 0~1 전체가 덮는 월드 한 변의 길이(m) × 1m당 텍셀 수
        float uvSideInMeters = Mathf.Sqrt(metric) * scale;
        int size = Mathf.ClosestPowerOfTwo(Mathf.CeilToInt(uvSideInMeters * _texelsPerMeter));
        size = Mathf.Clamp(size, _minSize, _maxSize);

        Debug.Log($"[Paintabale] {gameObject.name}: uvSide={uvSideInMeters:F1}m -> {size}");
        return size;
    }

    float CalcBumpNoiseScale()
    {
        if (_bumpNoiseOverrider > 0) return _bumpNoiseOverrider;

        MeshFilter mf = GetComponent<MeshFilter>();
        float metric = mf != null && mf.sharedMesh != null ? mf.sharedMesh.GetUVDistributionMetric(_bumpNoiseUVChanel) : 0f;
        if (metric <= 0f || float.IsInfinity(metric) || float.IsNaN(metric)) return 20f;

        Vector3 Ls = transform.lossyScale;
        float scale = Mathf.Max(Mathf.Abs(Ls.x), Mathf.Abs(Ls.y), Mathf.Abs(Ls.z));

        _bumpScale = _bumpNoisePerMeter * scale * Mathf.Sqrt(metric);

        return _bumpNoisePerMeter * scale * Mathf.Sqrt(metric);
    }

    public Color CheckPaintColor(RaycastHit hit)
    {
        if (hit.collider.gameObject == this.gameObject)
        {
           
            Vector2 uv = hit.textureCoord2;

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

    static void ClearRenderTexture(RenderTexture rt)
    {
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = prev;
    }

    void OnDisable()
    {
        _maskRenderTexture.Release();
        _uvIslandsRenderTexture.Release();
        _extendIslandsRenderTexture.Release();
        _supportTexture.Release();
    }
}

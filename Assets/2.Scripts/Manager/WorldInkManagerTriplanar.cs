using UnityEngine;

/// <summary>
/// Triplanar 방식 잉크 매니저
/// XZ(바닥), XY(앞뒤 벽), ZY(좌우 벽) 3개의 텍스처로 모든 표면을 처리
/// </summary>
public class WorldInkManagerTriplanar : Singleton<WorldInkManagerTriplanar>
{
    [Header("맵 설정")]
    [Tooltip("맵의 XZ 시작 좌표 (좌하단 월드 좌표)")]
    public Vector2 _mapOffset = new Vector2(-50f, -50f);

    [Tooltip("맵의 가로/세로 크기 (월드 유닛)")]
    public float _mapSize = 100f;

    [Tooltip("맵의 높이 범위 (Y축 최솟값)")]
    public float _mapMinY = 0f;

    [Tooltip("맵의 높이 크기 (월드 유닛)")]
    public float _mapHeight = 30f;

    [Header("텍스처 설정")]
    public int _textureSize = 2048;

    [Header("셰이더")]
    public Shader _brushShader;

    // 3방향 투영 텍스처
    // XZ : 바닥/천장  (수평면)
    // XY : 앞뒤 벽   (Z축 법선)
    // ZY : 좌우 벽   (X축 법선)
    RenderTexture _inkTexXZ;
    RenderTexture _inkTexXY;
    RenderTexture _inkTexZY;

    Material _brushMaterial;

    // 외부 읽기 전용
    public RenderTexture InkTexXZ => _inkTexXZ;
    public RenderTexture InkTexXY => _inkTexXY;
    public RenderTexture InkTexZY => _inkTexZY;
    public Vector2       MapOffset => _mapOffset;
    public float         MapSize   => _mapSize;
    public float         MapMinY   => _mapMinY;
    public float         MapHeight => _mapHeight;

    static readonly int _brushPosID      = Shader.PropertyToID("_BrushPos");
    static readonly int _brushRadiusID   = Shader.PropertyToID("_BrushRadius");
    static readonly int _brushColorID    = Shader.PropertyToID("_BrushColor");
    static readonly int _brushHardnessID = Shader.PropertyToID("_BrushHardness");

    public override void Awake()
    {
        base.Awake();

        _inkTexXZ = CreateInkTexture();
        _inkTexXY = CreateInkTexture();
        _inkTexZY = CreateInkTexture();

        _brushMaterial = new Material(_brushShader);
    }

    RenderTexture CreateInkTexture()
    {
        RenderTexture rt        = new RenderTexture(_textureSize, _textureSize, 0, RenderTextureFormat.ARGB32);
        rt.filterMode           = FilterMode.Bilinear;
        rt.wrapMode             = TextureWrapMode.Clamp;
        rt.Create();
        return rt;
    }

    /// <summary>
    /// 월드 좌표와 법선 벡터를 받아 해당 표면에 잉크를 칠합니다.
    /// hit.normal을 함께 넘겨야 방향별로 올바른 텍스처에 기록됩니다.
    /// </summary>
    public void Paint(Vector3 hitPoint, Vector3 hitNormal, Color inkColor, float radius = 1f, float hardness = 0.8f)
    {
        // 법선의 각 축 절댓값 = 해당 방향 텍스처에 기여하는 가중치
        // 예) 바닥(법선 위) → absNormal = (0, 1, 0) → XZ 텍스처에 100% 기여
        //     45도 경사면  → absNormal = (0, 0.7, 0.7) → XZ, XY 텍스처에 각 70% 기여
        Vector3 absNormal = new Vector3(
            Mathf.Abs(hitNormal.x),
            Mathf.Abs(hitNormal.y),
            Mathf.Abs(hitNormal.z)
        );

        // XZ 텍스처 : 바닥/천장 (Y축 법선 기여도)
        // u = X좌표, v = Z좌표
        if (absNormal.y > 0.01f)
        {
            float u = (hitPoint.x - _mapOffset.x) / _mapSize;
            float v = (hitPoint.z - _mapOffset.y) / _mapSize;

            if (u >= 0f && u <= 1f && v >= 0f && v <= 1f)
            {
                // 가중치만큼 알파를 낮춰서 칠함 (경사면에서 자연스러운 블렌딩)
                Color weightedColor = new Color(inkColor.r, inkColor.g, inkColor.b, inkColor.a * absNormal.y);
                Blit(_inkTexXZ, u, v, radius / _mapSize, weightedColor, hardness);
            }
        }

        // XY 텍스처 : 앞뒤 벽 (Z축 법선 기여도)
        // u = X좌표, v = Y좌표(높이)
        if (absNormal.z > 0.01f)
        {
            float u = (hitPoint.x - _mapOffset.x) / _mapSize;
            float v = (hitPoint.y - _mapMinY)      / _mapHeight;

            if (u >= 0f && u <= 1f && v >= 0f && v <= 1f)
            {
                Color weightedColor = new Color(inkColor.r, inkColor.g, inkColor.b, inkColor.a * absNormal.z);
                Blit(_inkTexXY, u, v, radius / _mapSize, weightedColor, hardness);
            }
        }

        // ZY 텍스처 : 좌우 벽 (X축 법선 기여도)
        // u = Z좌표, v = Y좌표(높이)
        if (absNormal.x > 0.01f)
        {
            float u = (hitPoint.z - _mapOffset.y) / _mapSize;
            float v = (hitPoint.y - _mapMinY)      / _mapHeight;

            if (u >= 0f && u <= 1f && v >= 0f && v <= 1f)
            {
                Color weightedColor = new Color(inkColor.r, inkColor.g, inkColor.b, inkColor.a * absNormal.x);
                Blit(_inkTexZY, u, v, radius / _mapSize, weightedColor, hardness);
            }
        }
    }

    void Blit(RenderTexture target, float u, float v, float uvRadius, Color inkColor, float hardness)
    {
        _brushMaterial.SetVector(_brushPosID,      new Vector4(u, v, 0f, 0f));
        _brushMaterial.SetFloat (_brushRadiusID,   uvRadius);
        _brushMaterial.SetColor (_brushColorID,    inkColor);
        _brushMaterial.SetFloat (_brushHardnessID, hardness);

        RenderTexture temp = RenderTexture.GetTemporary(target.descriptor);
        Graphics.Blit(target, temp, _brushMaterial);
        Graphics.Blit(temp, target);
        RenderTexture.ReleaseTemporary(temp);
    }

    void OnDrawGizmos()
    {
        Vector3 center = new Vector3(_mapOffset.x + _mapSize * 0.5f, _mapMinY + _mapHeight * 0.5f, _mapOffset.y + _mapSize * 0.5f);
        Vector3 size   = new Vector3(_mapSize, _mapHeight, _mapSize);

        Gizmos.color = new Color(0f, 0f, 1f, 0.15f);
        Gizmos.DrawCube(center, size);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(center, size);
    }

    void OnDisable()
    {
        _inkTexXZ?.Release();
        _inkTexXY?.Release();
        _inkTexZY?.Release();
    }
}

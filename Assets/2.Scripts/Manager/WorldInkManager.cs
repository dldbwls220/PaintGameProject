using UnityEngine;

/// <summary>
/// 맵 전체의 잉크 상태를 하나의 RenderTexture로 관리하는 매니저
/// World-Space XZ 좌표를 텍스처 UV로 변환해서 페인팅
/// </summary>
public class WorldInkManager : Singleton<WorldInkManager>
{
    [Header("맵 설정")]
    [Tooltip("맵의 XZ 시작 좌표 (좌하단 월드 좌표)")]
    public Vector2 _mapOffset = new Vector2(-50f, -50f);

    [Tooltip("맵의 가로/세로 크기 (월드 유닛). 예: 100이면 100x100 영역 커버")]
    public float _mapSize = 100f;

    [Header("텍스처 설정")]
    [Tooltip("잉크 텍스처 해상도. 클수록 세밀하지만 메모리 사용량 증가")]
    public int _textureSize = 2048;

    [Header("셰이더")]
    public Shader _brushShader;

    // 맵 전체 잉크 상태를 저장하는 텍스처 (외부에서 읽기 전용)
    public RenderTexture InkTexture  => _inkRenderTexture;
    public Vector2       MapOffset   => _mapOffset;
    public float         MapSize     => _mapSize;

    RenderTexture _inkRenderTexture;
    Material      _brushMaterial;

    // 브러시 셰이더 프로퍼티 ID (문자열 조회 비용 절약)
    static readonly int _brushPosID      = Shader.PropertyToID("_BrushPos");
    static readonly int _brushRadiusID   = Shader.PropertyToID("_BrushRadius");
    static readonly int _brushColorID    = Shader.PropertyToID("_BrushColor");
    static readonly int _brushHardnessID = Shader.PropertyToID("_BrushHardness");

    public override void Awake()
    {
        base.Awake();

        // 맵 전체를 커버하는 잉크 RenderTexture 생성
        _inkRenderTexture             = new RenderTexture(_textureSize, _textureSize, 0, RenderTextureFormat.ARGB32);
        _inkRenderTexture.filterMode  = FilterMode.Bilinear;
        _inkRenderTexture.wrapMode    = TextureWrapMode.Clamp;
        _inkRenderTexture.Create();

        _brushMaterial = new Material(_brushShader);
    }

    /// <summary>
    /// 월드 좌표에 잉크를 칠합니다.
    /// InkProjectile의 PaintInk()에서 이 메서드를 호출하면 됩니다.
    /// </summary>
    /// <param name="hitPoint">레이캐스트 hit.point (월드 좌표)</param>
    /// <param name="inkColor">칠할 잉크 색상</param>
    /// <param name="radius">브러시 반경 (월드 유닛)</param>
    /// <param name="hardness">경계 선명도 0~1 (1에 가까울수록 선명)</param>
    public void Paint(Vector3 hitPoint, Color inkColor, float radius = 1f, float hardness = 0.8f)
    {
        // ① 월드 XZ 좌표 → 텍스처 UV 변환 (0~1 정규화)
        float u = (hitPoint.x - _mapOffset.x) / _mapSize;
        float v = (hitPoint.z - _mapOffset.y) / _mapSize;

        // ② 맵 범위 밖이면 무시
        if (u < 0f || u > 1f || v < 0f || v > 1f)
        {
            Debug.LogWarning($"[WorldInkManager] 맵 범위 밖 페인트 시도: {hitPoint}");
            return;
        }

        // ③ 브러시 반경도 UV 공간으로 변환
        // 월드 1유닛이 텍스처에서 (1 / _mapSize) 비율을 차지함
        float uvRadius = radius / _mapSize;

        // ④ 브러시 셰이더 파라미터 설정
        _brushMaterial.SetVector(_brushPosID,      new Vector4(u, v, 0f, 0f));
        _brushMaterial.SetFloat (_brushRadiusID,   uvRadius);
        _brushMaterial.SetColor (_brushColorID,    inkColor);
        _brushMaterial.SetFloat (_brushHardnessID, hardness);

        // ⑤ 임시 텍스처에 현재 잉크 상태 + 새 브러시를 합성 후 다시 저장
        // Blit: _inkRenderTexture를 읽어서 _brushMaterial로 처리 → temp에 저장
        // 그 다음 temp → _inkRenderTexture로 복사
        RenderTexture temp = RenderTexture.GetTemporary(_inkRenderTexture.descriptor);
        Graphics.Blit(_inkRenderTexture, temp, _brushMaterial);
        Graphics.Blit(temp, _inkRenderTexture);
        RenderTexture.ReleaseTemporary(temp);

        Debug.Log($"[WorldInk] 페인트 성공 UV=({u:F3}, {v:F3}) radius={uvRadius:F4}");
    }

    void OnDrawGizmos()
    {
        // MapOffset은 Vector2(X, Z)이므로 Vector3로 변환 필요
        // DrawCube의 첫 번째 인자는 중심점이므로 mapSize * 0.5 만큼 이동
        Vector3 center = new Vector3(_mapOffset.x + _mapSize * 0.5f, 0f, _mapOffset.y + _mapSize * 0.5f);
        Vector3 size   = new Vector3(_mapSize, 0f, _mapSize);

        Gizmos.color = new Color(0f, 0f, 1f, 0.2f);
        Gizmos.DrawCube(center, size);         // 반투명 채우기

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(center, size);     // 테두리
    }

    void OnDisable()
    {
        if (_inkRenderTexture != null)
            _inkRenderTexture.Release();
    }
}

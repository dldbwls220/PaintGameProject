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
}

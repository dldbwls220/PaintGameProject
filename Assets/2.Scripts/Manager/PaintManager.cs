using UnityEngine;
using UnityEngine.Rendering;

public class PaintManager : MonoBehaviour
{
    static PaintManager _uniqueInstance;

    public Shader _texturePaint;
    public Shader _extendIslands;

    int _prepareUVID = Shader.PropertyToID("_PrepareUV");
    int _positionID = Shader.PropertyToID("_PainterPosition");
    int _hardnessID = Shader.PropertyToID("_Hardness");
    int _strengthID = Shader.PropertyToID("_Strength");
    int _radiusID = Shader.PropertyToID("_Radius");
    int _blendOpID = Shader.PropertyToID("_BlendOp");
    int _colorID = Shader.PropertyToID("_PainterColor");
    int _textureID = Shader.PropertyToID("_MainTex");
    int _uvOffsetID = Shader.PropertyToID("_OffsetUV");
    int _uvIslandsID = Shader.PropertyToID("_UVIslands");

    Material _paintMaterial;
    Material _extendMaterial;

    CommandBuffer _command;

    MaterialPropertyBlock _propBlock;

    public static PaintManager _instance => _uniqueInstance;

    private void Awake()
    {
        _uniqueInstance = this;

        _paintMaterial = new Material(_texturePaint);
        _extendMaterial = new Material(_extendIslands);
        _command = new CommandBuffer();
        _command.name = "CommmandBuffer - " + gameObject.name;

        _propBlock = new MaterialPropertyBlock();
    }
    public void initTextures(Paintabale paintable)
    {
        RenderTexture uvIslands = paintable.getUVIslands();
        Renderer rend = paintable.getRenderer();

        _paintMaterial.SetFloat(_prepareUVID, 1);
        _command.SetRenderTarget(uvIslands);

        // 모든 서브메시의 UV 아일랜드를 초기화
        int subMeshCount = rend.GetComponent<MeshFilter>() != null
            ? rend.GetComponent<MeshFilter>().sharedMesh.subMeshCount
            : 1;

        for (int i = 0; i < subMeshCount; i++)
        {
            _command.DrawRenderer(rend, _paintMaterial, i);
        }

        Graphics.ExecuteCommandBuffer(_command);
        _command.Clear();
    }


    public void paint(Paintabale paintable, Vector3 pos, float radius = 1f, float hardness = .5f, float strength = .5f, Color? color = null)
    {
        RenderTexture mask = paintable.getmask();
        RenderTexture uvIslands = paintable.getUVIslands();
        RenderTexture extend = paintable.getExtend();
        RenderTexture support = RenderTexture.GetTemporary(mask.descriptor);
        Renderer rend = paintable.getRenderer();

        Mesh mesh = paintable.GetComponent<MeshFilter>().sharedMesh; // 메쉬 가져오기
        Matrix4x4 matrix = paintable.transform.localToWorldMatrix;   // 변환 행렬

        _propBlock.Clear();
        _propBlock.SetFloat(_prepareUVID, 0);
        _propBlock.SetVector(_positionID, pos);
        _propBlock.SetFloat(_hardnessID, hardness);
        _propBlock.SetFloat(_strengthID, strength);
        _propBlock.SetFloat(_radiusID, radius);
        _propBlock.SetTexture(_textureID, support);
        _propBlock.SetColor(_colorID, color ?? Color.red);

        _command.Clear();
        _command.Blit(mask, support);        // 칠하기 전 상태를 먼저 복사
        _command.SetRenderTarget(mask);

        // 모든 서브메시에 페인트 적용
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            _command.DrawMesh(mesh, matrix, _paintMaterial, i, 0, _propBlock);
        }

        _extendMaterial.SetFloat(_uvOffsetID, paintable._extendsIslandOffset);
        _extendMaterial.SetTexture(_uvIslandsID, uvIslands);

        _command.SetRenderTarget(extend);
        _command.Blit(mask, extend, _extendMaterial);

        Graphics.ExecuteCommandBuffer(_command);
        _command.Clear();
        RenderTexture.ReleaseTemporary(support);

        //Debug.Log($"[Paint] obj={paintable.name} pos={pos} radius={radius} scale={paintable.transform.lossyScale}");

        //Graphics.ExecuteCommandBuffer(_command);
    }
}

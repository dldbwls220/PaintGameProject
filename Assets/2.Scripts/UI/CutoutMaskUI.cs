using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class CutoutMaskUI : Image
{
    static readonly int _stencilCompID = Shader.PropertyToID("_StencilComp");

    Material _cutoutMaterial;
    Material _sourceMaterial;

    public override Material materialForRendering
    {
        get
        {
            Material source = base.materialForRendering;

            // 원본이 바뀌었을 때만 새로 만든다 (매 호출마다 new 하면 머티리얼이 계속 쌓인다)
            if (_cutoutMaterial == null || _sourceMaterial != source)
            {
                DestroyCutoutMaterial();
                _sourceMaterial = source;
                _cutoutMaterial = new Material(source);
                _cutoutMaterial.SetInt(_stencilCompID, (int)CompareFunction.NotEqual);
            }

            return _cutoutMaterial;
        }
    }

    protected override void OnDestroy()
    {
        DestroyCutoutMaterial();
        base.OnDestroy();
    }

    void DestroyCutoutMaterial()
    {
        if (_cutoutMaterial == null) return;

        if (Application.isPlaying) Destroy(_cutoutMaterial);
        else DestroyImmediate(_cutoutMaterial);

        _cutoutMaterial = null;
    }
}

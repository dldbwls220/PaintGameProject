using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// WorldInk/DisplayZone 셰이더용 인스펙터.
/// "표면 타입" 드롭다운(Opaque / Transparent)에 맞춰 블렌드 상태,
/// 렌더 큐, 키워드, 그림자 패스를 자동으로 갱신한다.
/// </summary>
public class WorldInkDisplayZoneGUI : ShaderGUI
{
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        base.OnGUI(materialEditor, properties);

        foreach (var target in materialEditor.targets)
        {
            if (target is Material material)
                ApplySurface(material);
        }
    }

    public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
    {
        base.AssignNewShaderToMaterial(material, oldShader, newShader);
        ApplySurface(material);
    }

    private static void ApplySurface(Material mat)
    {
        bool transparent = mat.HasProperty("_Surface")   && mat.GetFloat("_Surface")   > 0.5f;
        bool alphaClip   = mat.HasProperty("_AlphaClip")  && mat.GetFloat("_AlphaClip") > 0.5f;

        if (transparent)
        {
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetShaderPassEnabled("ShadowCaster", false);
        }
        else
        {
            mat.SetFloat("_SrcBlend", (float)BlendMode.One);
            mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
            mat.SetFloat("_ZWrite", 1f);
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.renderQueue = alphaClip ? (int)RenderQueue.AlphaTest : (int)RenderQueue.Geometry;
            mat.SetShaderPassEnabled("ShadowCaster", true);
        }

        if (alphaClip) mat.EnableKeyword("_ALPHATEST_ON");
        else           mat.DisableKeyword("_ALPHATEST_ON");
    }
}

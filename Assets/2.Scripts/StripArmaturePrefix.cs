using UnityEditor;
using UnityEngine;

public class StripArmaturePrefix
{
    //[MenuItem("Tools/Strip 'Armature-' Prefix From Selected Clip")]
    //static void Strip()
    //{
    //    var clip = Selection.activeObject as AnimationClip;
    //    if (clip == null) { Debug.LogError("AnimationClip을 먼저 선택하세요."); return; }

    //    var newClip = Object.Instantiate(clip);
    //    newClip.ClearCurves();

    //    foreach (var binding in AnimationUtility.GetCurveBindings(clip))
    //    {
    //        var b = binding;
    //        if (b.path == "Armature") continue;               // 래퍼 노드 자체 커브는 버림
    //        if (b.path.StartsWith("Armature/"))
    //            b.path = b.path.Substring("Armature/".Length); // 접두사 제거
    //        AnimationUtility.SetEditorCurve(newClip, b, AnimationUtility.GetEditorCurve(clip, binding));
    //    }

    //    string path = AssetDatabase.GenerateUniqueAssetPath("Assets/3.Animation/Result/EmoteStript/" + clip.name + "_stripped.anim");
    //    AssetDatabase.CreateAsset(newClip, path);
    //    AssetDatabase.SaveAssets();
    //    Debug.Log("생성됨: " + path);
    //}
}
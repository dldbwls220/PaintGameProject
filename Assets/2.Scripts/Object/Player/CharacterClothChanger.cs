using UnityEngine;
using DefineEnum;
using System.Collections.Generic;

public class CharacterClothChanger : MonoBehaviour
{
    [SerializeField] Transform _characterRootBone;
    [SerializeField] Transform _characterHeadRoot;

    public void EquipClothing(GameObject clothPrefab, CustomizeState state)
    {

        switch (state)
        {
            case CustomizeState.Hair:
                SetHair(clothPrefab);
                break;
            case CustomizeState.Eyebrow:
                SetClothes(clothPrefab);
                break;
            case CustomizeState.Head:
                SetHair(clothPrefab);
                break;
            case CustomizeState.Shirts:
                SetClothes(clothPrefab);
                break;
        }


    }

    void SetHair(GameObject clothPrefab)
    {
        if (_characterHeadRoot == null)
        {
            Debug.LogError("머리카락을 장착하려 하지만 _characterHeadRoot(Head 본)가 지정되지 않았습니다.");
            return;
        }

        GameObject hairObj = Instantiate(clothPrefab, _characterHeadRoot);

        return;
    }

    void SetClothes(GameObject clothPrefab)
    {
        GameObject clothObj = Instantiate(clothPrefab, transform);

        SkinnedMeshRenderer[] clothRender = clothObj.GetComponentsInChildren<SkinnedMeshRenderer>();
        if (clothRender == null) return;

        Dictionary<string, Transform> boneMap = new Dictionary<string, Transform>();
        foreach (Transform bone in _characterRootBone.GetComponentsInChildren<Transform>())
        {
            if (!boneMap.ContainsKey(bone.name))
                boneMap.Add(bone.name, bone);
        }

        foreach (SkinnedMeshRenderer renderer in clothRender)
        {
            if (renderer == null) continue;

            Transform[] oldBones = renderer.bones;
            Transform[] newBones = new Transform[oldBones.Length];

            for (int i = 0; i < oldBones.Length; i++)
            {
                string boneName = oldBones[i].name;
                if (boneMap.TryGetValue(boneName, out Transform targetBone))
                {
                    newBones[i] = targetBone; // 캐릭터의 뼈대로 대체
                }
                else
                {
                    Debug.LogWarning($"{renderer.name}의 뼈대 '{boneName}'를 캐릭터에게서 찾을 수 없습니다.");
                    newBones[i] = oldBones[i];
                }
            }

            renderer.bones = newBones;

            if (renderer.rootBone != null)
            {
                string rootBoneName = renderer.rootBone.name;
                if (boneMap.TryGetValue(rootBoneName, out Transform targetRootBone))
                {
                    renderer.rootBone = targetRootBone; // 캐릭터의 루트본으로 교체 성공!
                }
                else
                {
                    Debug.LogWarning($"{renderer.name}의 루트 본 '{rootBoneName}'을 캐릭터에게서 찾을 수 없습니다.");
                }
            }
            else
            {
                // 기존 옷에 루트본이 없었다면 캐릭터의 최상위 루트본을 기본값으로 지정해 줍니다.
                renderer.rootBone = _characterRootBone;
            }


        }

        Transform clothingArmature = clothObj.transform.Find("Armature");
        if (clothingArmature != null)
        {
            Destroy(clothingArmature.gameObject);
        }
    }
}

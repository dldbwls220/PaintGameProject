using UnityEngine;
using DefineEnum;
using System.Collections.Generic;
using DefineStructure;

public class CharacterClothChanger : MonoBehaviour
{
    [SerializeField] Transform _characterRootBone;
    [SerializeField] Transform _characterHeadRoot;

    [SerializeField] HairState _hairState;
    [SerializeField] EyebrowsState _eyebrowsState;
    [SerializeField] HeadState _headState;
    [SerializeField] BodyState _bodyState;
    [SerializeField] BottomState _bottomState;
    [SerializeField] ShoeState _shoeState;

    readonly List<GameObject> _equippedObjects = new List<GameObject>();

    public void SetCustomization(PlayerCustomization custom)
    {
        _headState = custom._head;
        _bodyState = custom._cloth;
        _shoeState = custom._shoes;
        _hairState = custom._hair;
        _eyebrowsState = custom._eyebrows;
        _bottomState = custom._bottom;

        // 이 함수는 스폰 직후(기본값)와 RPC로 실제 값이 도착했을 때(OnCustomizationChanged) 두 번 이상 호출되므로,
        // 매번 새 Dictionary로 만들고 이전에 장착했던 오브젝트를 지운 뒤 다시 장착해야 한다.
        var customDic = new Dictionary<CustomizeState, string>
        {
            { CustomizeState.Head, _headState.ToString() },
            { CustomizeState.Shirts, _bodyState.ToString() },
            { CustomizeState.Shoes, _shoeState.ToString() },
            { CustomizeState.Hair, _hairState.ToString() },
            { CustomizeState.Eyebrows, _eyebrowsState.ToString() },
            { CustomizeState.Bottom, _bottomState.ToString() },
        };

        ClearEquipped();

        foreach (KeyValuePair<CustomizeState, string> pair in customDic)
        {
            if (pair.Value == "Count") continue;

            Equip(pair.Key, pair.Value);

            if (pair.Key == CustomizeState.Shoes)
            {
                Equip(pair.Key, pair.Value + "_R");
            }
        }
    }

    void ClearEquipped()
    {
        foreach (GameObject go in _equippedObjects)
        {
            if (go != null) Destroy(go);
        }
        _equippedObjects.Clear();
    }

    void Equip(CustomizeState state, string itemName)
    {
        GameObject prefab = Resources.Load<GameObject>("Object/Customizing/" + state.ToString() + "/" + itemName);

        if (prefab == null)
        {
            Debug.LogWarning($"커스터마이징 리소스를 찾을 수 없습니다: Object/Customizing/{state}/{itemName}");
            return;
        }

        GameObject equipped = EquipClothing(prefab, state);

        if (equipped != null) _equippedObjects.Add(equipped);
    }

    public GameObject EquipClothing(GameObject clothPrefab, CustomizeState state)
    {
        switch (state)
        {
            case CustomizeState.Hair:
            case CustomizeState.Head:
                return SetHair(clothPrefab);
            case CustomizeState.Eyebrows:
            case CustomizeState.Shirts:
            case CustomizeState.Shoes:
            case CustomizeState.Bottom:
                return SetClothes(clothPrefab);
            default:
                return null;
        }
    }

    GameObject SetHair(GameObject clothPrefab)
    {
        if (_characterHeadRoot == null)
        {
            Debug.LogError("머리카락을 장착하려 하지만 _characterHeadRoot(Head 본)가 지정되지 않았습니다.");
            return null;
        }

        return Instantiate(clothPrefab, _characterHeadRoot);
    }

    GameObject SetClothes(GameObject clothPrefab)
    {
        GameObject clothObj = Instantiate(clothPrefab, _characterRootBone.parent.parent);

        clothObj.transform.localPosition = Vector3.zero;
        clothObj.transform.localRotation = Quaternion.identity;
        clothObj.transform.localScale = Vector3.one;

        SkinnedMeshRenderer[] clothRender = clothObj.GetComponentsInChildren<SkinnedMeshRenderer>();
        if (clothRender == null) return clothObj;

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
            clothingArmature.SetParent(null);
            Destroy(clothingArmature.gameObject);
        }

        return clothObj;
    }
}

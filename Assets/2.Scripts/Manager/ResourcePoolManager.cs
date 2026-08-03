using System.Collections.Generic;
using UnityEngine;
using DefineEnum;

public class ResourcePoolManager : Singleton<ResourcePoolManager>
{
    Dictionary<PoolDataType, Dictionary<string, object>> _allPoolDatas;

    public void AllLoadResources()
    {
        _allPoolDatas = new Dictionary<PoolDataType, Dictionary<string, object>>();
        LoadClothGear();
        LoadHeadGear();
        LoadShoesGear();
    }

    void LoadHeadGear()
    {
        Dictionary<string, object> headGear = new Dictionary<string, object>();
        Sprite[] imgs = Resources.LoadAll<Sprite>("Sprite/Item/HeadGearIcons");
        for (int i = 0; i < imgs.Length; i++)
            headGear.Add(imgs[i].name, imgs[i]);

        _allPoolDatas.Add(PoolDataType.HEADGEARIMG, headGear);
    }

    void LoadClothGear()
    {
        Dictionary<string, object> clothGear = new Dictionary<string, object>();
        Sprite[] imgs = Resources.LoadAll<Sprite>("Sprite/Item/ClothGearIcons");
        for (int i = 0; i < imgs.Length; i++)
            clothGear.Add(imgs[i].name, imgs[i]);

        _allPoolDatas.Add(PoolDataType.CLOTHGEARIMG, clothGear);
    }

    void LoadShoesGear()
    {
        Dictionary<string, object> shoesGear = new Dictionary<string, object>();
        Sprite[] imgs = Resources.LoadAll<Sprite>("Sprite/Item/ShoesGearIcons");
        for (int i = 0; i < imgs.Length; i++)
            shoesGear.Add(imgs[i].name, imgs[i]);

        _allPoolDatas.Add(PoolDataType.SHOESGEARIMG, shoesGear);
    }

    public T Get<T>(PoolDataType type, string index)
    {
        if (!_allPoolDatas.ContainsKey(type))
            return default(T);

        if (!_allPoolDatas[type].ContainsKey(index))
            return default(T);

        return (T)_allPoolDatas[type][index];
    }

    public T Get<T>(PoolDataType type, int index)
    {
        return Get<T>(type, index.ToString());
    }
}

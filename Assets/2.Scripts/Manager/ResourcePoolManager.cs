using System.Collections.Generic;
using UnityEngine;
using DefineEnum;

public class ResourcePoolManager : Singleton<ResourcePoolManager>
{
    Dictionary<PoolDataType, Dictionary<string, object>> _allPoolDatas;

    public void AllLoadResources()
    {
        if (_allPoolDatas != null) return;

        _allPoolDatas = new Dictionary<PoolDataType, Dictionary<string, object>>();
        LoadClothGear();
        LoadHeadGear();
        LoadShoesGear();
        LoadHairIcon();
        LoadEyebrowsIcon();
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

    void LoadHairIcon()
    {
        Dictionary<string, object> hairIcon = new Dictionary<string, object>();
        Sprite[] imgs = Resources.LoadAll<Sprite>("Sprite/Item/HairIcons");
        for (int i = 0; i < imgs.Length; i++)
            hairIcon.Add(imgs[i].name, imgs[i]);

        _allPoolDatas.Add(PoolDataType.HAIRICONIMG, hairIcon);
    }

    void LoadEyebrowsIcon()
    {
        Dictionary<string, object> eyebrowsIcon = new Dictionary<string, object>();
        Sprite[] imgs = Resources.LoadAll<Sprite>("Sprite/Item/EyebrowsIcons");
        for (int i = 0; i < imgs.Length; i++)
            eyebrowsIcon.Add(imgs[i].name, imgs[i]);

        _allPoolDatas.Add(PoolDataType.EYEBROWSICONIMG, eyebrowsIcon);
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

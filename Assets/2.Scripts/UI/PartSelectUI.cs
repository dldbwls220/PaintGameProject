using UnityEngine;
using UnityEngine.UI;
using DefineEnum;

public class PartSelectUI : MonoBehaviour
{
    [Header("Item Image")]
    [SerializeField] Image _headImg;
    [SerializeField] Image _clothImg;
    [SerializeField] Image _shoesImg;

    private void Update()
    {
        CheckItemImage();
    }

    void CheckItemImage()
    {
        string name = PlayerCustomizeManager.instance.Customization._head.ToString();

        _headImg.sprite = ResourcePoolManager.instance.Get<Sprite>(PoolDataType.HEADGEARIMG, name);

        name = PlayerCustomizeManager.instance.Customization._cloth.ToString();

        _clothImg.sprite = ResourcePoolManager.instance.Get<Sprite>(PoolDataType.CLOTHGEARIMG, name);

        name = PlayerCustomizeManager.instance.Customization._shoes.ToString();

        _shoesImg.sprite = ResourcePoolManager.instance.Get<Sprite>(PoolDataType.SHOESGEARIMG, name);
    }
}

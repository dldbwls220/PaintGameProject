using UnityEngine;
using UnityEngine.UI;
using DefineEnum;

public class ItemSlotUI : MonoBehaviour
{
    [SerializeField] Image _itemImg;

    [Header("For Debuging")]
    [SerializeField] BodyState _bodyState;
    [SerializeField] HeadState _headState;
    [SerializeField] ShoeState _shoeState;

    public void InitWnd(BodyState body = BodyState.Count, HeadState head = HeadState.Count, ShoeState shoe = ShoeState.Count)
    {
        Sprite icon = null;

        if (body != BodyState.Count)
        {
            icon = ResourcePoolManager.instance.Get<Sprite>(PoolDataType.CLOTHGEARIMG, body.ToString());
        }
        else if (head != HeadState.Count)
        {
            icon = ResourcePoolManager.instance.Get<Sprite>(PoolDataType.HEADGEARIMG, head.ToString());
        }
        else if (shoe != ShoeState.Count)
        {
            icon = ResourcePoolManager.instance.Get<Sprite>(PoolDataType.SHOESGEARIMG, shoe.ToString());
        }

        _bodyState = body; _headState = head; _shoeState = shoe;

        _itemImg.sprite = icon;
    }

    public void SetItem()
    {
        if (_bodyState != BodyState.Count)
        {
            PlayerCustomizeManager.instance.SetBody(_bodyState);
        }
        else if (_headState != HeadState.Count)
        {
            PlayerCustomizeManager.instance.SetHead(_headState);
        }
        else if (_shoeState != ShoeState.Count)
        {
            PlayerCustomizeManager.instance.SetShoe(_shoeState);
        }
    }
}

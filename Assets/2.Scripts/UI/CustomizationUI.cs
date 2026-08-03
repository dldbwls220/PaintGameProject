using UnityEngine;
using UnityEngine.UI;
using DefineEnum;

public class CustomizationUI : MonoBehaviour
{
    [Header("Parts Window")]
    [SerializeField] GameObject _headGearWnd;
    [SerializeField] GameObject _clothGearWnd;
    [SerializeField] GameObject _shoedGearWmd;
    [SerializeField] GameObject _styleWnd;

    [Header("Gear Contents")]
    [SerializeField] GameObject _headContent;
    [SerializeField] GameObject _clothContent;
    [SerializeField] GameObject _shoesContent;

    [Header("Resource")]
    [SerializeField] GameObject _itemSlotObj;

    public static CustomizationUI _instance;

    private void Awake()
    {
        _instance = this;

        ResourcePoolManager.instance.AllLoadResources();
        InitCustomizationUI();
    }

    public void InitCustomizationUI()
    {
        _headGearWnd.SetActive(true);
        _clothGearWnd.SetActive(false);
        _shoedGearWmd.SetActive(false);
        _styleWnd.SetActive(false);

        SetAllGears();
    }

    #region[Open N Close]

    public void OpenHeadGearWnd()
    {
        _headGearWnd.SetActive(true);
    }

    public void OpenClothGearWnd()
    {
        _clothGearWnd.SetActive(true);
    }

    public void OpenShoesGearWnd()
    {
        _shoedGearWmd.SetActive(true);
    }

    public void OpenStyleWnd()
    {
        _styleWnd.SetActive(true);
    }

    public void CloseHeadGearWnd()
    {
        _headGearWnd.SetActive(false);
    }

    public void CloseClothGearWnd()
    {
        _clothGearWnd.SetActive(false);
    }

    public void CloseShoesGearWnd()
    {
        _shoedGearWmd.SetActive(false);
    }

    public void CloseStyleWnd()
    {
        _styleWnd.SetActive(false);
    }

    #endregion[Open N Close]

    public void SetAllGears()
    {
        for (int i = 0; i < (int)HeadState.Count; i++)
        {
            GameObject go = Instantiate(_itemSlotObj, _headContent.transform);
            ItemSlotUI itemui = go.GetComponent<ItemSlotUI>();

            itemui.InitWnd(head: (HeadState)i);
        }

        for (int i = 0; i < (int)BodyState.Count; i++)
        {
            GameObject go = Instantiate(_itemSlotObj, _clothContent.transform);
            ItemSlotUI itemui = go.GetComponent<ItemSlotUI>();

            itemui.InitWnd((BodyState)i);
        }

        for (int i = 0; i < (int)ShoeState.Count; i++)
        {
            GameObject go = Instantiate(_itemSlotObj, _shoesContent.transform);
            ItemSlotUI itemui = go.GetComponent<ItemSlotUI>();

            itemui.InitWnd(shoe: (ShoeState)i);
        }
    }
}

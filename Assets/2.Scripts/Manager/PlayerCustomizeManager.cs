using DefineEnum;
using DefineStructure;

public class PlayerCustomizeManager : Singleton<PlayerCustomizeManager>
{
    PlayerCustomization _customization;

    public PlayerCustomization Customization => _customization;


    private void Start()
    {
        //test
        initDefaultcustom();
    }

    public void initDefaultcustom()
    {
        _customization = PlayerCustomization.Default;
    }

    public void SetHead(HeadState head)
    {
        PlayerCustomization c = _customization;
        c._head = head;
        _customization = c;
    }

    public void SetBody(BodyState body)
    {
        PlayerCustomization c = _customization;
        c._cloth = body;
        _customization = c;
    }

    public void SetShoe(ShoeState shoe)
    {
        PlayerCustomization c = _customization;
        c._shoes = shoe;
        _customization = c;
    }
}

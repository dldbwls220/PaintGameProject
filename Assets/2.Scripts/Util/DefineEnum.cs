using UnityEngine;

namespace DefineEnum
{
    #region[캐릭터]

    public enum AniState
    {
        Idle,
        Walk,
        Run,
        Jump,
        Shoot,
        Morph_toSquid,
        Morph_toHuman,
        Slowed,
        Slowed_Walk,

        Squid_Idle = 11,
        Squid_Walk,
        Squid_Jump
    }

    public enum FormState
    {
        Inkling,
        Half,
        Squid
    }

    public enum CustomizeState
    {
        Hair,
        Eyebrows,
        EyeColor,
        Head,
        Shirts,
        Shoes,
        Bottom
    }

    #endregion[캐릭터]

    #region[소리]

    public enum LoopName
    {
        Count
    }

    public enum BGMName
    {
        Count
    }

    public enum  SFXName
    {
        Shtr_Shot_00,
        Hit_Inkling_00,

        Count
    }

    #endregion[소리]

    #region[커스터마이징]

    public enum HairState
    {
        Har_SQD000_F_TeamE,
        Har_SQD001_F_TeamE,
        Har_SQD003_F_TeamE,
        Har_SQD004_F_TeamE
    }

    public enum EyebrowsState
    {
        Eyb_SQD000_F_TeamC,
        Eyb_SQD001_F_TeamC,
        Eyb_SQD002_F_TeamC,
        Eyb_SQD003_F_TeamC
    }

    public enum HeadState
    {
        Headlamp_Helmet,
        Howdy_Hat,
        Retro_BluFocals,
        Skull_Bandana,
        Stay_Crusty_Cap
    }

    public enum BodyState
    {
        Annaki_Choker_Tee,
        Cream_Tundra_Fleece,
        Gray_Hoodie,
        Orca_Bolero,
        Takoroka_Nylon_Vintage
    }

    public enum BottomState
    {
        Btm_000_F,
        Btm_001_F,
        Btm_002_F,
        Btm_003_F,
        Btm_004_F
    }

    public enum ShoeState
    {
        Skipjack_Work_Boots
    }

    #endregion[커스터마이징]

    #region[풀링]

    public enum InkProjectileState
    {
        InkBullet,
        InkSplash,
        InkHit
    }

    #endregion[풀링]
}

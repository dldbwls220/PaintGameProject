using UnityEngine;

namespace DefineEnum
{
    #region[잉클링]

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

    #endregion[잉클링]

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

    public enum PlayerVoiceSFX3DName
    {
        Voice_SquidGirl_Dead_00,
        Voice_SquidGirl_Dead_01,
        Voice_SquidGirl_Dead_02,
        Voice_SquidGirl_Dead_03,
        Voice_SquidGirl_Dead_04,
        Voice_SquidGirl_Dead_05,
        Voice_SquidGirl_Dead_06,

        Count
    }

    public enum PlayerSFX3DName
    {
        DeadSplash00,

        Count
    }

    public enum PlayerVoiceSFXName
    {
        Voice_SquidGirl_Damage_00,
        Voice_SquidGirl_Damage_01,
        Voice_SquidGirl_Damage_02,
        Voice_SquidGirl_Damage_03,
        Voice_SquidGirl_Damage_04,
        Voice_SquidGirl_Damage_05,
        Voice_SquidGirl_Damage_06,
        Voice_SquidGirl_Damage_07,

        Voice_SquidGirl_Soul_00,
        Voice_SquidGirl_Soul_01,

        Count
    }

    public enum PlayerSFXName
    {
        Damage00,
        damageIncLoop00,
        RespawnStart00,
        RespawnEnd00,
        SurprisedMix00,
        ToHumanMix00,
        ToSquidMix00,

        Count
    }

    public enum ProjectileSFX3DName
    {
        Swish00,
        Swish01,
        Swish02,
        Swish03,

        Count
    }

    public enum ProjectileSFXName
    {
        HitEffectiveCommon02,

        inkHit00,
        inkHit01,
        inkHit02,
        inkHit03,
        inkHit04,
        inkHit05,
        inkHit06,
        inkHit07,

        inkHitSplash00,
        inkHitSplash01,
        inkHitSplash02,
        inkHitSplash03,

        Count
    }

    public enum WeaponSFX3DName
    {
        MachineGun00,

        Count
    }

    public enum WeaponSFXName
    {
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

    #region[잉크]

    public enum InkProjectileState
    {
        InkBullet,
        InkSplash,
        InkHit
    }

    #endregion[잉크]

    #region[무기]

    public enum MainWeaponState
    {
        Shooter,
        Charger,
        Roller
    }

    public enum SubWeaponState
    {

    }

    #endregion[무기]

    #region[팀]

    public enum TeamState
    {
        Team1,
        Team2
    }

    #endregion[팀]

    #region[씬]

    public enum SceneState
    {
        StartScene,
        LobbyScene,
        Port_Mackerel
    }

    #endregion[씬]
}

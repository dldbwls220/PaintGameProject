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
        Eyebrow,
        EyeColor,
        Head,
        Shirts,
        Shoes
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
}

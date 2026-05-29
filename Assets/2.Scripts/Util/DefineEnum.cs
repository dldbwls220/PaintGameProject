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
}

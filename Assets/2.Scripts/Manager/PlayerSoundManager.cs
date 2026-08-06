using DefineEnum;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerSoundManager : MonoBehaviour
{
    [Header("SwimSoundSetting")]
    [SerializeField] float _swimVolumeSpeed;
    [SerializeField] float _maxSwimVolue;

    bool _prevMorphingSquid, _prevMorphingInkling, _prevRespawing, _prevPendingRespawn;

    public struct SoundState 
    {
        public bool isSquid;
        public bool isMorphingSquid;
        public bool isMorphingInkling;
        public bool isGrounded;
        public bool isShooting;
        public bool isMoving;
        public bool isSwimming;
        public bool isSlowed;
        public bool isAlive;
        public bool isRespawning;
        public bool isWallClimb;

        public bool hasInputAuthority;

        public bool wasMorphingSquid;
        public bool wasMorphingInkling;
        public bool wasRespwaning;
        public bool wasPendingRespawn;

        public Health health;
    }

    public void UpdateSound(in SoundState state)
    {
        PlaySwimSFX(state);
        PlayinklingSFX(state);
        PlaySlowedSFX(state);
    }

    public void SetLoopSFX()
    {
        GameSoundManager.instance.PlayerSFXLoop(PlayerSFXLoopName.SwimmingInInk_loopable);
    }

    public void PlaySwimSFX(in SoundState state)
    {
        GameSoundManager.instance.PlayerSFXLoop(PlayerSFXLoopName.SwimmingInInk_loopable);

        if (state.isAlive && state.isSquid && state.isMoving && (state.isWallClimb || (state.isSwimming && state.isGrounded)))
            GameSoundManager.instance.SetLoopVolume(PlayerSFXLoopName.SwimmingInInk_loopable,
                Mathf.MoveTowards(GameSoundManager.instance.GetLoopVolume(PlayerSFXLoopName.SwimmingInInk_loopable), _maxSwimVolue, _swimVolumeSpeed * Time.deltaTime));
        else
            OffLoopSFX(PlayerSFXLoopName.SwimmingInInk_loopable);
    }

    public void PlaySlowedSFX(in SoundState state)
    {
        GameSoundManager.instance.PlayerSFXLoop(PlayerSFXLoopName.Slowed_loopable);

        if (state.isAlive && !state.isSquid && state.isSlowed)
            GameSoundManager.instance.SetLoopVolume(PlayerSFXLoopName.Slowed_loopable,
                Mathf.MoveTowards(GameSoundManager.instance.GetLoopVolume(PlayerSFXLoopName.Slowed_loopable), _maxSwimVolue, _swimVolumeSpeed * Time.deltaTime));
        else
            OffLoopSFX(PlayerSFXLoopName.Slowed_loopable);
    }

    public void OffLoopSFX(PlayerSFXLoopName name)
    {
        GameSoundManager.instance.SetLoopVolume(name, 0);
    }

    void PlayinklingSFX(in SoundState state)
    {
        if (state.hasInputAuthority)
        {
            if (state.wasMorphingSquid && !state.isMorphingSquid)
                GameSoundManager.instance.PlayerSFX(PlayerSFXName.ToSquidMix00, volume: 0.4f);
            if (state.wasMorphingInkling && !state.isMorphingInkling)
                GameSoundManager.instance.PlayerSFX(PlayerSFXName.ToHumanMix00, volume: 0.4f);
            if (!state.wasPendingRespawn && state.health._pendingRespawn)
                GameSoundManager.instance.PlayerSFX(PlayerSFXName.RespawnStart00, volume: 0.4f);
            if (state.wasRespwaning && !state.health._nowRespawing)
                GameSoundManager.instance.PlayerSFX(PlayerSFXName.RespawnEnd00, volume: 0.4f);
        }
    }
}

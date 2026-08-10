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

    // 플레이어별 전용 루프 채널. GameSoundManager의 공유 AudioSource를 쓰면
    // 여러 플레이어의 매 프레임 갱신이 서로의 볼륨을 덮어써 소리가 나오지 않는 문제가 있었다.
    AudioSource _swimLoopSource;
    AudioSource _slowedLoopSource;

    bool _prevMorphingSquid, _prevMorphingInkling, _prevRespawing, _prevPendingRespawn;

    void Awake()
    {
        _swimLoopSource = gameObject.AddComponent<AudioSource>();
        _swimLoopSource.playOnAwake = false;
        _swimLoopSource.loop = true;
        _swimLoopSource.volume = 0;

        _slowedLoopSource = gameObject.AddComponent<AudioSource>();
        _slowedLoopSource.playOnAwake = false;
        _slowedLoopSource.loop = true;
        _slowedLoopSource.volume = 0;
    }

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
        GameSoundManager.instance.PlayerSFXLoop(PlayerSFXLoopName.SwimmingInInk_loopable, _swimLoopSource);
        GameSoundManager.instance.PlayerSFXLoop(PlayerSFXLoopName.Slowed_loopable, _slowedLoopSource);
    }

    public void PlaySwimSFX(in SoundState state)
    {
        if (!state.hasInputAuthority)
        {
            OffLoopSFX(_swimLoopSource);
            return;
        }

        GameSoundManager.instance.PlayerSFXLoop(PlayerSFXLoopName.SwimmingInInk_loopable, _swimLoopSource);

        if (state.isAlive && state.isSquid && state.isMoving && (state.isWallClimb || (state.isSwimming && state.isGrounded)))
            GameSoundManager.instance.SetLoopVolume(_swimLoopSource,
                Mathf.MoveTowards(GameSoundManager.instance.GetLoopVolume(_swimLoopSource), _maxSwimVolue, _swimVolumeSpeed * Time.deltaTime));
        else
            OffLoopSFX(_swimLoopSource);
    }

    public void PlaySlowedSFX(in SoundState state)
    {
        if (!state.hasInputAuthority)
        {
            OffLoopSFX(_slowedLoopSource);
            return;
        }

        GameSoundManager.instance.PlayerSFXLoop(PlayerSFXLoopName.Slowed_loopable, _slowedLoopSource);

        if (state.isAlive && !state.isSquid && state.isSlowed)
            GameSoundManager.instance.SetLoopVolume(_slowedLoopSource,
                Mathf.MoveTowards(GameSoundManager.instance.GetLoopVolume(_slowedLoopSource), _maxSwimVolue, _swimVolumeSpeed * Time.deltaTime));
        else
            OffLoopSFX(_slowedLoopSource);
    }

    public void OffLoopSFX(AudioSource source)
    {
        GameSoundManager.instance.SetLoopVolume(source, 0);
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

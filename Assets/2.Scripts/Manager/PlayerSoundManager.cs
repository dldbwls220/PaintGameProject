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
    }

    public void PlaySwimSFX()
    {
        GameSoundManager.instance.PlayerSFXLoop(PlayerSFXName.SwimmingInInk_loopable, volume: 0);
        GameSoundManager.instance._swimDESC._volum = Mathf.MoveTowards(GameSoundManager.instance._swimDESC._volum, _maxSwimVolue, _swimVolumeSpeed * Time.deltaTime);
    }

    public void OffLoopSFX()
    {
        GameSoundManager.instance._swimDESC._volum = 0;
    }
}

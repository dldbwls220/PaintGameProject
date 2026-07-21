using DefineEnum;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerSoundManager : MonoBehaviour
{
    Dictionary<PlayerSFX3DName, AudioClip> _playerSFX3DDoc;
    Dictionary<PlayerSFXName, AudioClip> _playerSFXDoc;

    [SerializeField] AudioSource _SFX3D;
    [SerializeField] AudioSource _SFX;

    private void Awake()
    {
        _playerSFX3DDoc = new Dictionary<PlayerSFX3DName, AudioClip> ();
        _playerSFXDoc = new Dictionary<PlayerSFXName, AudioClip>();
    }

    public void LoadAllSound()
    {
        if (_playerSFX3DDoc.Count > 1) return;

        string path = "Sound/SFX/";

        int count = (int)PlayerSFX3DName.Count;
        for (int i = 0; i < count; i++)
        {
            PlayerSFX3DName name = (PlayerSFX3DName)i;
            AudioClip clip = Resources.Load<AudioClip>(path + "Voice/InklingGirl/" + name);
            _playerSFX3DDoc.Add(name, clip);
        }

        count = (int)PlayerSFXName.Count;
        for (int i = 0; i < count; i++)
        {
            PlayerSFXName name = (PlayerSFXName)i;
            AudioClip clip = Resources.Load<AudioClip>(path + "Voice/InklingGirl/" + name);
            _playerSFXDoc.Add(name, clip);
        }
    }

    public void PlayerSFX(PlayerSFXName name)
    {
        if (!_playerSFXDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip은 없습니다", name);
            return;
        }
        _SFX.PlayOneShot(_playerSFXDoc[name]);
    }

    public void PlayerSFX3D(PlayerSFX3DName name)
    {
        if (!_playerSFX3DDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip은 없습니다", name);
            return;
        }
        _SFX3D.PlayOneShot(_playerSFX3DDoc[name]);
    }

    public void SetVolume(PlayerAudioSourceState state ,float volume)
    {
        switch (state)
        {
            case PlayerAudioSourceState.SFX3D:
                _SFX3D.volume = volume;
                break;
            case PlayerAudioSourceState.SFX:
                _SFX.volume = volume;
                break;
        }
    }

    public void SetMute(PlayerAudioSourceState state, bool mute)
    {
        switch (state)
        {
            case PlayerAudioSourceState.SFX3D:
                _SFX3D.mute = mute;
                break;
            case PlayerAudioSourceState.SFX:
                _SFX.mute = mute;
                break;
        }
    }

    public void SetLoop(PlayerAudioSourceState state, bool Loop)
    {
        switch (state)
        {
            case PlayerAudioSourceState.SFX3D:
                _SFX3D.loop = Loop;
                break;
            case PlayerAudioSourceState.SFX:
                _SFX.loop = Loop;
                break;
        }
    }

    public void PauseSFX(PlayerAudioSourceState state)
    {
        switch (state)
        {
            case PlayerAudioSourceState.SFX3D:
                _SFX3D.Pause();
                break;
            case PlayerAudioSourceState.SFX:
                _SFX.Pause();
                break;
        }
    }

    public void UnpauseSFX(PlayerAudioSourceState state)
    {
        switch (state)
        {
            case PlayerAudioSourceState.SFX3D:
                _SFX3D.UnPause();
                break;
            case PlayerAudioSourceState.SFX:
                _SFX.UnPause();
                break;
        }
    }

    public void StopSFX(PlayerAudioSourceState state)
    {
        switch (state)
        {
            case PlayerAudioSourceState.SFX3D:
                _SFX3D.Stop();
                break;
            case PlayerAudioSourceState.SFX:
                _SFX.Stop();
                break;
        }
    }

    public void InitSFX(PlayerAudioSourceState state, float vol, bool mute, bool loop = false)
    {
        SetVolume(state, vol);
        SetMute(state, mute);
        SetLoop(state, loop);
    }
}

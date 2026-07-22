using DefineEnum;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileSoundManager : MonoBehaviour
{
    Dictionary<ProjectileSFX3DName, AudioClip> _projectileSFX3DDoc;
    Dictionary<ProjectileSFXName, AudioClip> _projectileSFXDoc;

    AudioSource _SFX3D;
    AudioSource _SFX;

    private void Awake()
    {
        _projectileSFX3DDoc = new Dictionary<ProjectileSFX3DName, AudioClip>();
        _projectileSFXDoc = new Dictionary<ProjectileSFXName, AudioClip>();
    }

    public void LoadAllSound()
    {
        if (_projectileSFX3DDoc.Count > 1) return;

        string path = "Sound/SFX/";

        int count = (int)ProjectileSFX3DName.Count;
        for (int i = 0; i < count; i++)
        {
            ProjectileSFX3DName name = (ProjectileSFX3DName)i;
            AudioClip clip = Resources.Load<AudioClip>(path + "BulletNInkSounds/" + name);
            _projectileSFX3DDoc.Add(name, clip);
        }

        count = (int)ProjectileSFXName.Count;
        for (int i = 0; i < count; i++)
        {
            ProjectileSFXName name = (ProjectileSFXName)i;
            AudioClip clip = Resources.Load<AudioClip>(path + "BulletNInkSounds/" + name);
            _projectileSFXDoc.Add(name, clip);
        }
    }

    public void ProjectileSFX(ProjectileSFXName name , AudioSource source)
    {
        if (!_projectileSFXDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip은 없습니다", name);
            return;
        }

        source.PlayOneShot(_projectileSFXDoc[name]);
    }

    public void ProjectileSFX3D(ProjectileSFX3DName name, AudioSource source)
    {
        if (!_projectileSFX3DDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip은 없습니다", name);
            return;
        }
        source.PlayOneShot(_projectileSFX3DDoc[name]);
    }

    public void SetVolume(PlayerAudioSourceState state, float volume)
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

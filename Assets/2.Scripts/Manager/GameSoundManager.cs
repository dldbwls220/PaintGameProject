using DefineEnum;
using DefineStructure;
using System.Collections.Generic;
using UnityEngine;

public class GameSoundManager : Singleton<GameSoundManager>
{
    Dictionary<ProjectileSFX3DName, AudioClip> _projectileSFX3DDoc;
    Dictionary<ProjectileSFXName, AudioClip> _projectileSFXDoc;

    Dictionary<PlayerVoiceSFX3DName, AudioClip> _playerVoiceSFX3DDoc;
    Dictionary<PlayerVoiceSFXName, AudioClip> _playerVoiceSFXDoc;
    Dictionary<PlayerSFX3DName, AudioClip> _playerSFX3DDoc;
    Dictionary<PlayerSFXName, AudioClip> _playerSFXDoc;

    Dictionary<WeaponSFX3DName, AudioClip> _weaponSFX3DDoc;
    public AudioPlayerDESC _SFXDESC;
    AudioSource _sfxPlayer;
    public AudioPlayerDESC _BGMDESC;
    AudioSource _bgmPlayer;
    public AudioPlayerDESC _lastMinBGMDESC;
    AudioSource _lastminbgmPlayer;

    public override void Awake()
    {
        base.Awake();

        _projectileSFX3DDoc = new Dictionary<ProjectileSFX3DName, AudioClip>();
        _projectileSFXDoc = new Dictionary<ProjectileSFXName, AudioClip>();

        _playerVoiceSFX3DDoc = new Dictionary<PlayerVoiceSFX3DName, AudioClip>();
        _playerSFX3DDoc = new Dictionary<PlayerSFX3DName, AudioClip>();
        _playerVoiceSFXDoc = new Dictionary<PlayerVoiceSFXName, AudioClip>();
        _playerSFXDoc = new Dictionary<PlayerSFXName, AudioClip>();

        _weaponSFX3DDoc = new Dictionary<WeaponSFX3DName, AudioClip>();

        _sfxPlayer = gameObject.AddComponent<AudioSource>();
        _bgmPlayer = gameObject.AddComponent<AudioSource>(); ;
        _lastminbgmPlayer = gameObject.AddComponent<AudioSource>();

        _SFXDESC = new AudioPlayerDESC(_sfxPlayer, 1, false, false);
        _BGMDESC = new AudioPlayerDESC(_bgmPlayer, 1, false, false);
        _lastMinBGMDESC = new AudioPlayerDESC(_lastminbgmPlayer, 1, false, false);
    }

    private void Start()
    {
        LoadAllSound(); // 테스트용
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

        path = "Sound/SFX/";

        count = (int)PlayerVoiceSFX3DName.Count;
        for (int i = 0; i < count; i++)
        {
            PlayerVoiceSFX3DName name = (PlayerVoiceSFX3DName)i;
            AudioClip clip = Resources.Load<AudioClip>(path + "Voice/InklingGirl/" + name);
            _playerVoiceSFX3DDoc.Add(name, clip);
        }

        for (int i = 0; i < count; i++)
        {
            PlayerSFX3DName name = (PlayerSFX3DName)i;
            AudioClip clip = Resources.Load<AudioClip>(path + "Other/" + name);
            _playerSFX3DDoc.Add(name, clip);
        }

        count = (int)PlayerVoiceSFXName.Count;
        for (int i = 0; i < count; i++)
        {
            PlayerVoiceSFXName name = (PlayerVoiceSFXName)i;
            AudioClip clip = Resources.Load<AudioClip>(path + "Voice/InklingGirl/" + name);
            _playerVoiceSFXDoc.Add(name, clip);
        }

        for (int i = 0; i < count; i++)
        {
            PlayerSFXName name = (PlayerSFXName)i;
            AudioClip clip = Resources.Load<AudioClip>(path + "Other/" + name);
            _playerSFXDoc.Add(name, clip);
        }

        count = (int)WeaponSFX3DName.Count;
        for (int i = 0; i < count; i++)
        {
            WeaponSFX3DName name = (WeaponSFX3DName)i;
            AudioClip clip = Resources.Load<AudioClip>(path + "Splattershot/" + name);
            _weaponSFX3DDoc.Add(name, clip);
        }
    }

    public void PlayerVoiceSFX(PlayerVoiceSFXName name, AudioSource source = null, float volume = 1f)
    {
        if (!_playerVoiceSFXDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip이 없습니다", name);
            return;
        }

        if (source == null) source = _sfxPlayer;

        source.PlayOneShot(_playerVoiceSFXDoc[name], volume);
    }

    public void PlayerVoiceSFX3D(PlayerVoiceSFX3DName name, AudioSource source, float volume = 1f)
    {
        if (!_playerVoiceSFX3DDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip이 없습니다", name);
            return;
        }
        source.PlayOneShot(_playerVoiceSFX3DDoc[name], volume);
    }

    public void PlayerSFX(PlayerSFXName name, AudioSource source = null, float volume = 1f)
    {
        if (!_playerSFXDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip이 없습니다", name);
            return;
        }
        if (source == null) source = _sfxPlayer;

        source.PlayOneShot(_playerSFXDoc[name], volume);
    }

    public void PlayerSFX3D(PlayerSFX3DName name, AudioSource source, float volume = 1f)
    {
        if (!_playerSFX3DDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip이 없습니다", name);
            return;
        }
        source.PlayOneShot(_playerSFX3DDoc[name], volume);
    }

    public void ProjectileSFX(ProjectileSFXName name, AudioSource source = null, float volume = 1f)
    {
        if (!_projectileSFXDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip이 없습니다", name);
            return;
        }
        if (source == null) source = _sfxPlayer;

        source.PlayOneShot(_projectileSFXDoc[name], volume);
    }

    public void ProjectileSFX3D(ProjectileSFX3DName name, AudioSource source, float volume = 1f)
    {
        if (!_projectileSFX3DDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip이 없습니다", name);
            return;
        }
        source.PlayOneShot(_projectileSFX3DDoc[name], volume);
    }

    public void WeaponSFX3D(WeaponSFX3DName name, AudioSource source, float volume = 1f)
    {
        if (!_weaponSFX3DDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip이 없습니다", name);
            return;
        }
        source.PlayOneShot(_weaponSFX3DDoc[name], volume);
    }
}

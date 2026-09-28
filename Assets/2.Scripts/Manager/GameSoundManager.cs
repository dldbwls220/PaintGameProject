using DefineEnum;
using DefineStructure;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class GameSoundManager : Singleton<GameSoundManager>
{
    AudioMixer _audioMixerGroup;
    AudioMixerGroup _bgmGroup;
    AudioMixerGroup _sfxGroup;

    const string Master_Volume = "MasterMixer";

    Dictionary<ProjectileSFX3DName, AudioClip> _projectileSFX3DDoc;
    Dictionary<ProjectileSFXName, AudioClip> _projectileSFXDoc;

    Dictionary<PlayerVoiceSFX3DName, AudioClip> _playerVoiceSFX3DDoc;
    Dictionary<PlayerVoiceSFXName, AudioClip> _playerVoiceSFXDoc;
    Dictionary<PlayerSFX3DName, AudioClip> _playerSFX3DDoc;
    Dictionary<PlayerSFXName, AudioClip> _playerSFXDoc;
    Dictionary<PlayerSFXLoopName, AudioClip> _playerSFXLoopDoc;

    Dictionary<UIBGMName, AudioClip> _UIBGMDoc;
    Dictionary<OpeningBGMName, AudioClip> _openingBGMDoc;
    Dictionary<NormalBGMName, AudioClip> _normalBGMDoc;
    Dictionary<SquidSisters, AudioClip> _squidSistersBGMDoc;
    Dictionary<Tentacles, AudioClip> _tentaclesBGMDoc;
    Dictionary<DeepCut, AudioClip> _deepCutBGMDoc;
    Dictionary<NowOrNever, AudioClip> _nowOrNeverBGMDoc;
    Dictionary<ResultBGM, AudioClip> _resultBGMDoc;

    Dictionary<WeaponSFX3DName, AudioClip> _weaponSFX3DDoc;

    public AudioPlayerDESC _SFXDESC;
    AudioSource _sfxPlayer;
    public AudioPlayerDESC _UiBGMDESC;
    AudioSource _uibgmPlayer;
    public AudioPlayerDESC _GameBGMDESC;
    AudioSource _gamebgmPlayer;
    public AudioPlayerDESC _NowOrNeverDESC;
    AudioSource _nowOrNeverPlayer;
    public AudioPlayerDESC _ResultBGMDESC;
    AudioSource _resultPlayer;

    public override void Awake()
    {
        base.Awake();

        _projectileSFX3DDoc = new Dictionary<ProjectileSFX3DName, AudioClip>();
        _projectileSFXDoc = new Dictionary<ProjectileSFXName, AudioClip>();

        _playerVoiceSFX3DDoc = new Dictionary<PlayerVoiceSFX3DName, AudioClip>();
        _playerSFX3DDoc = new Dictionary<PlayerSFX3DName, AudioClip>();
        _playerVoiceSFXDoc = new Dictionary<PlayerVoiceSFXName, AudioClip>();
        _playerSFXDoc = new Dictionary<PlayerSFXName, AudioClip>();
        _playerSFXLoopDoc = new Dictionary<PlayerSFXLoopName, AudioClip>();

        _UIBGMDoc = new Dictionary<UIBGMName, AudioClip>();
        _openingBGMDoc = new Dictionary<OpeningBGMName, AudioClip>();
        _normalBGMDoc = new Dictionary<NormalBGMName, AudioClip>();
        _squidSistersBGMDoc = new Dictionary<SquidSisters, AudioClip>();
        _tentaclesBGMDoc = new Dictionary<Tentacles, AudioClip>();
        _deepCutBGMDoc = new Dictionary<DeepCut, AudioClip>();
        _nowOrNeverBGMDoc = new Dictionary<NowOrNever, AudioClip>();
        _resultBGMDoc = new Dictionary<ResultBGM, AudioClip>();

        _weaponSFX3DDoc = new Dictionary<WeaponSFX3DName, AudioClip>();

        _sfxPlayer = gameObject.AddComponent<AudioSource>();
        _uibgmPlayer = gameObject.AddComponent<AudioSource>();
        _gamebgmPlayer = gameObject.AddComponent<AudioSource>();
        _nowOrNeverPlayer = gameObject.AddComponent<AudioSource>();
        _resultPlayer = gameObject.AddComponent<AudioSource>();

        _SFXDESC = new AudioPlayerDESC(_sfxPlayer, 1, false, false);
        _UiBGMDESC = new AudioPlayerDESC(_uibgmPlayer, 1, false, false);
        _GameBGMDESC = new AudioPlayerDESC(_gamebgmPlayer, 0.7f, false, false);
        _NowOrNeverDESC = new AudioPlayerDESC(_nowOrNeverPlayer, 0.7f, false, false);
        _ResultBGMDESC = new AudioPlayerDESC(_resultPlayer, 0.7f, false, false);
    }

    public void LoadAllSound()
    {
        if (_projectileSFX3DDoc.Count > 1) return;

        SetAudioMixerGroup();

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

        count = (int)PlayerSFX3DName.Count;
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

        count = (int)PlayerSFXName.Count;
        for (int i = 0; i < count; i++)
        {
            PlayerSFXName name = (PlayerSFXName)i;
            AudioClip clip = Resources.Load<AudioClip>(path + "Other/" + name);
            _playerSFXDoc.Add(name, clip);
        }

        count = (int)PlayerSFXLoopName.Count;
        for (int i = 0; i < count; i++)
        {
            PlayerSFXLoopName name = (PlayerSFXLoopName)i;
            AudioClip clip = Resources.Load<AudioClip>(path + "Other/" + name);
            _playerSFXLoopDoc.Add(name, clip);

        }

        count = (int)WeaponSFX3DName.Count;
        for (int i = 0; i < count; i++)
        {
            WeaponSFX3DName name = (WeaponSFX3DName)i;
            AudioClip clip = Resources.Load<AudioClip>(path + "Splattershot/" + name);
            _weaponSFX3DDoc.Add(name, clip);
        }

        path = "Sound/Music/";

        count = (int)UIBGMName.Count;
        for (int i = 0; i < count; i++)
        {
            UIBGMName name = (UIBGMName)i;
            AudioClip clip = Resources.Load<AudioClip>(path + "OtherScene/" + name);
            _UIBGMDoc.Add(name, clip);
        }

        count = (int)OpeningBGMName.Count;
        for (int i = 0; i < count; i++)
        {
            OpeningBGMName name = (OpeningBGMName)i;
            AudioClip clip = Resources.Load<AudioClip>(path + "Battle/Opening/" + name);
            _openingBGMDoc.Add(name, clip);
        }

        count = (int)NormalBGMName.Count;
        for (int i = 0; i < count; i++)
        {
            NormalBGMName name = (NormalBGMName)i;
            AudioClip clip = Resources.Load<AudioClip>(path + "Battle/" + name);
            _normalBGMDoc.Add(name, clip);
        }

        count = (int)SquidSisters.Count;
        for (int i = 0; i < count; i++)
        {
            SquidSisters name = (SquidSisters)i;
            AudioClip clip = Resources.Load<AudioClip>(path + "Battle/SquidSisters/" + name);
            _squidSistersBGMDoc.Add(name, clip);
        }

        count = (int)Tentacles.Count;
        for (int i = 0; i < count; i++)
        {
            Tentacles name = (Tentacles)i;
            AudioClip clip = Resources.Load<AudioClip>(path + "Battle/Tentacles/" + name);
            _tentaclesBGMDoc.Add(name, clip);
        }

        count = (int)DeepCut.Count;
        for (int i = 0; i < count; i++)
        {
            DeepCut name = (DeepCut)i;
            AudioClip clip = Resources.Load<AudioClip>(path + "Battle/DeepCut/" + name);
            _deepCutBGMDoc.Add(name, clip);
        }

        count = (int)NowOrNever.Count;
        for (int i = 0; i < count; i++)
        {
            NowOrNever name = (NowOrNever)i;
            AudioClip clip = Resources.Load<AudioClip>(path + "Battle/NowOrNever/" + name);
            _nowOrNeverBGMDoc.Add(name, clip);
        }

        count = (int)ResultBGM.Count;
        for (int i = 0; i < count; i++)
        {
            ResultBGM name = (ResultBGM)i;
            AudioClip clip = Resources.Load<AudioClip>(path + "Battle/Result/" + name);
            _resultBGMDoc.Add(name, clip);
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

    public void UIBGM(UIBGMName name)
    {
        if (!_UIBGMDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip이 없습니다", name);
            return;
        }
        _uibgmPlayer.clip = _UIBGMDoc[name];
        _uibgmPlayer.Play();
    }

    public void OpeningBGM(OpeningBGMName name)
    {
        if (!_openingBGMDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip이 없습니다", name);
            return;
        }
        _uibgmPlayer.clip = _openingBGMDoc[name];
        _uibgmPlayer.Play();
    }

    public void GameBGMNormal(NormalBGMName name)
    {
        if (!_normalBGMDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip이 없습니다", name);
            return;
        }
        _gamebgmPlayer.clip = _normalBGMDoc[name];
        _gamebgmPlayer.Play();
    }

    public void GameBGMSquidSisters(SquidSisters name)
    {
        if (!_squidSistersBGMDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip이 없습니다", name);
            return;
        }
        _gamebgmPlayer.clip = _squidSistersBGMDoc[name];
        _gamebgmPlayer.Play();
    }
    public void GameBGMSquidTentacles(Tentacles name)
    {
        if (!_tentaclesBGMDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip이 없습니다", name);
            return;
        }
        _gamebgmPlayer.clip = _tentaclesBGMDoc[name];
        _gamebgmPlayer.Play();
    }
    public void GameBGMSquidDeepCut(DeepCut name)
    {
        if (!_deepCutBGMDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip이 없습니다", name);
            return;
        }
        _gamebgmPlayer.clip = _deepCutBGMDoc[name];
        _gamebgmPlayer.Play();
    }

    public void NowOrNeverBGM(NowOrNever name)
    {
        if (!_nowOrNeverBGMDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip이 없습니다", name);
            return;
        }
        _nowOrNeverPlayer.clip = _nowOrNeverBGMDoc[name];
        _nowOrNeverPlayer.Play();
    }

    public void Result(ResultBGM name)
    {
        if(!_resultBGMDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip이 없습니다", name);
            return;
        }
        _resultPlayer.clip = _resultBGMDoc[name];
        _resultPlayer.Play();
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

    // source는 재생 주체(플레이어)마다 별도로 소유해야 한다 — 공유 AudioSource를 쓰면
    // 여러 플레이어의 매 프레임 볼륨 갱신이 서로를 덮어써서 소리가 끊기거나 안 들리게 된다.
    public void PlayerSFXLoop(PlayerSFXLoopName name, AudioSource source, float volume = 0)
    {
        if (!_playerSFXLoopDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip이 없습니다", name);
            return;
        }

        if (volume > 0)
            source.volume = volume;


        if (source.isPlaying && source.clip == _playerSFXLoopDoc[name]) return;

        source.clip = _playerSFXLoopDoc[name];
        source.loop = true;
        source.Play();
    }

    public void SetLoopVolume(AudioSource source, float volume)
    {
        if (volume < 0)
        {
            source.volume = 0;
            source.mute = true;
        }
        else if (volume > 1)
        {
            source.volume = 1;
            source.mute = false;
        }
        else
        {
            source.volume = volume;
            source.mute = false;
        }
    }

    public float GetLoopVolume(AudioSource source)
    {
        return source.volume;
    }

    public void UpdateVolume(float value, MixerState state)
    {
        if(PlayerCustomizeManager.instance == null) return;

        PlayerCustomizeManager.instance.UpdateVolume(value, state);
    }

    // 믹서 에셋과 그룹을 한 번만 로드해 캐싱
    bool LoadMixer()
    {
        if (_audioMixerGroup != null) return true;

        _audioMixerGroup = Resources.Load<AudioMixer>("AudioMixer/MasterAudioMixer");
        if (_audioMixerGroup == null)
        {
            Debug.LogError("MasterAudioMixer를 찾을 수 없습니다");
            return false;
        }

        _bgmGroup = _audioMixerGroup.FindMatchingGroups("BGM")[0];
        _sfxGroup = _audioMixerGroup.FindMatchingGroups("SFX")[0];
        return true;
    }

    void SetAudioMixerGroup()
    {
        if (!LoadMixer()) return;

        _SFXDESC._output(_sfxGroup);

        _UiBGMDESC._output(_bgmGroup);

        _GameBGMDESC._output(_bgmGroup);

        _NowOrNeverDESC._output(_bgmGroup);

        _ResultBGMDESC._output(_bgmGroup);
    }

    // 외부(플레이어 프리팹 등)에서 생성한 AudioSource를 믹서 그룹에 연결
    public void SetOutput(AudioSource source, bool isSFX = true)
    {
        if (source == null || !LoadMixer()) return;

        source.outputAudioMixerGroup = isSFX ? _sfxGroup : _bgmGroup;
    }
}

using DefineEnum;
using DefineStructure;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : TSingleton<SoundManager>
{
    Dictionary<LoopName, AudioClip> _loopClipDoc;
    Dictionary<BGMName, AudioClip> _bgmClipDoc;
    Dictionary<SFXName, AudioClip> _sfxClipDoc;

    public AudioPlayerDESC _loopDESC;
    AudioSource _loopPlayer;
    public AudioPlayerDESC _bgmDESC;
    AudioSource _bgmPlayer;
    public AudioPlayerDESC _sfxDESC;
    AudioSource _sfxPlayer;

    private void Awake()
    {
        _loopClipDoc = new Dictionary<LoopName, AudioClip>();
        _bgmClipDoc = new Dictionary<BGMName, AudioClip>();
        _sfxClipDoc = new Dictionary<SFXName, AudioClip>();

        _loopPlayer = gameObject.AddComponent<AudioSource>();
        _bgmPlayer = gameObject.AddComponent<AudioSource>();
        _sfxPlayer = gameObject.AddComponent<AudioSource>();

        _loopDESC = new AudioPlayerDESC(_loopPlayer, 1, false);
        _bgmDESC = new AudioPlayerDESC(_bgmPlayer, 0.6f, false);
        _sfxDESC = new AudioPlayerDESC(_sfxPlayer, 1, false, false);
    }

    private void Start()
    {
        LoadAllSound();
    }

    public void LoadAllSound()
    {
        if (_loopClipDoc.Count > 1) return;

        string path = "Sound/";
        //int count = (int)BGMName.Count;
        //for (int i = 0; i < count; i++)
        //{
        //    LoopName name = (LoopName)i;
        //    AudioClip clip = Resources.Load<AudioClip>(path + "BGM/" + name);
        //    _loopClipDoc.Add(name, clip);
        //}
        //for (int i = 0; i < count; i++)
        //{
        //    BGMName name = (BGMName)i;
        //    AudioClip clip = Resources.Load<AudioClip>(path + "BGM/" + name);
        //    _bgmClipDoc.Add(name, clip);
        //}

        int count = (int)SFXName.Count;
        for (int i = 0; i < count; i++)
        {
            SFXName name = (SFXName)i;
            AudioClip clip = Resources.Load<AudioClip>(path + "SFX/" + name);
            _sfxClipDoc.Add(name, clip);
        }
    }

    public void PlayBGM(BGMName name)
    {
        if (!_bgmClipDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip은 없습니다", name);
            return;
        }
        _bgmPlayer.clip = _bgmClipDoc[name];
        _bgmPlayer.Play();
    }

    public void PlayLoop(LoopName name, double dspStartTime)
    {
        if (!_loopClipDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip은 없습니다", name);
            return;
        }
        _loopPlayer.clip = _loopClipDoc[name];
        _loopPlayer.PlayScheduled(dspStartTime);
    }

    public void PlaySFX(SFXName name)
    {
        //동시에 여러 사운드가 나와야 한다
        if (!_sfxClipDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip은 없습니다", name);
            return;
        }
        _sfxPlayer.PlayOneShot(_sfxClipDoc[name]);
    }

}

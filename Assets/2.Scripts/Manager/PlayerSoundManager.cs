using DefineEnum;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerSoundManager : MonoBehaviour
{
    Dictionary<PlayerSFX3DName, AudioClip> _playerSFX3DDoc;
    Dictionary<PlayerSFXName, AudioClip> _playerSFXDoc;
    Dictionary<PlayerSFX3DName, AudioClip> _otherSFX3DDoc;
    Dictionary<PlayerSFXName, AudioClip> _otherSFXDoc;

    [SerializeField] AudioSource _voiceSFX3D;
    [SerializeField] AudioSource _voiceSFX;
    [SerializeField] AudioSource _otherSFX3D;
    [SerializeField] AudioSource _otherSFX;

    private void Awake()
    {
        _playerSFX3DDoc = new Dictionary<PlayerSFX3DName, AudioClip>();
        _otherSFX3DDoc = new Dictionary<PlayerSFX3DName, AudioClip>();
        _playerSFXDoc = new Dictionary<PlayerSFXName, AudioClip>();
        _otherSFXDoc = new Dictionary<PlayerSFXName, AudioClip>();
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

        for (int i = 0; i < count; i++)
        {
            PlayerSFX3DName name = (PlayerSFX3DName)i;
            AudioClip clip = Resources.Load<AudioClip>(path + "Other/" + name);
            _otherSFX3DDoc.Add(name, clip);
        }

        count = (int)PlayerSFXName.Count;
        for (int i = 0; i < count; i++)
        {
            PlayerSFXName name = (PlayerSFXName)i;
            AudioClip clip = Resources.Load<AudioClip>(path + "Voice/InklingGirl/" + name);
            _playerSFXDoc.Add(name, clip);
        }

        for (int i = 0; i < count; i++)
        {
            PlayerSFXName name = (PlayerSFXName)i;
            AudioClip clip = Resources.Load<AudioClip>(path + "Other/" + name);
            _otherSFXDoc.Add(name, clip);
        }
    }

    public void PlayerSFX(PlayerSFXName name)
    {
        if (!_playerSFXDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip은 없습니다", name);
            return;
        }
        _voiceSFX.PlayOneShot(_playerSFXDoc[name]);
    }

    public void PlayerSFX3D(PlayerSFX3DName name)
    {
        if (!_playerSFX3DDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip은 없습니다", name);
            return;
        }
        _voiceSFX3D.PlayOneShot(_playerSFX3DDoc[name]);
    }

    public void OtherSFX(PlayerSFXName name)
    {
        if (!_otherSFXDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip은 없습니다", name);
            return;
        }
        _otherSFX.PlayOneShot(_otherSFXDoc[name]);
    }

    public void OtherSFX3D(PlayerSFX3DName name)
    {
        if (!_otherSFX3DDoc.ContainsKey(name))
        {
            Debug.LogFormat("{0} AudioClip은 없습니다", name);
            return;
        }
        _otherSFX3D.PlayOneShot(_otherSFX3DDoc[name]);
    }

    
}

using Fusion;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace DefineStructure
{
    public struct AudioPlayerDESC
    {
        AudioSource _player;

        public float _volum
        {
            get { return _player.volume; }
            set
            {
                if (value < 0)
                {
                    _player.volume = 0;
                    _player.mute = true;
                }
                else if (value > 1)
                {
                    _player.volume = 1;
                    _player.mute = false;
                }
                else
                {
                    _player.volume = value;
                    _player.mute = false;
                }

            }
        }

        public bool _mute
        {
            get { return _player.mute; }
            set { _player.mute = value; }
        }

        public bool _loop
        {
            get { return _player.loop; }
            set { _player.loop = value; }
        }

        public void _pause()
        {
            _player.Pause();
        }

        public void _unpause()
        {
            _player.UnPause();
        }

        public void _stop()
        {
            _player.Stop();
        }

        public AudioPlayerDESC(AudioSource audioS, float vol, bool mute, bool loop = true)
        {
            _player = audioS;
            _player.playOnAwake = false;
            _player.volume = vol;
            _player.mute = mute;
            _player.loop = loop;
        }
    }

    public struct PlayerData : INetworkStruct
    {
        [Networked, Capacity(24)]
        public string _nickName { get => default; set { } }
        public PlayerRef _playerRef;
        public Color _teamColor;
        public Color _enemyColor;
        public int _kills;
        public int _death;
        public int _lastKillTick;
        public int _statisticPostion;
        public bool _isAlive;
        public bool _isConnected;
    }
}

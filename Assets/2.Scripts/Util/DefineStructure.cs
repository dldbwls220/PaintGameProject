using DefineEnum;
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

        public void _clip(AudioClip clip)
        {
            _player.clip = clip;
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
        [Networked]
        public NetworkString<_32> _nickName { get; set; }
        public Color _teamColor;
        public Color _enemyColor;
        public int _teamIndex;
        public int _kills;
        public int _death;
        public int _lastKillTick;
        public int _statisticPostion;
        public float _myRespawnTime;
        public bool _isAlive;
        public bool _isConnected;
        public PlayerCustomization _custom;

        public string DisplayName => _nickName.Length == 0 ? "잉클링" : _nickName.Value;
    }

    public struct PlayerCustomization : INetworkStruct
    {
        public BodyState _cloth;
        public HeadState _head;
        public ShoeState _shoes;
        public HairState _hair;
        public EyebrowsState _eyebrows;
        public BottomState _bottom;

        public static PlayerCustomization Default => new PlayerCustomization
        {
            _cloth = BodyState.Tri_Shred_Tee,
            _head = HeadState.Headlamp_Helmet,
            _shoes = ShoeState.Skipjack_Work_Boots,
            _hair = HairState.Har_SQD000_F_TeamE,
            _eyebrows = EyebrowsState.Eyb_SQD000_F_TeamC,
            _bottom = BottomState.Btm_000_F
        };
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Demo2D
{

    [Serializable]
    public class Audio
    {
        public AudioType type;
        public string path;
        public uint crc;
        public AudioClip clip;
    }

    [Serializable]
    public class AudioPlayer
    {
        public AudioType type;
        public AudioSource audioSource;
    }

    public enum AudioType
    {
        Entrance,
        Idle,
        Attack,
        Jump,
        Land,
        Fell,
        Hit,
        Dead,
        GetUp,
        DoubleJump,
        SwordCharge,
        SwordArt,
        Focus,
        Heal,
        Spell,
        AttackReject,
        Talk,
        FootSteps,
        Falling,
        Explosion,
        Spit,
        Fly,
        Dash,
        AttackVoice,
        JumpVoice,
        FellVoice,
        HitVoice,
        DeadVoice,
        IdleVoice
    }

    public class MonoAudioModule : MonoBehaviour
    {
        private readonly Dictionary<AudioType, List<Audio>> _audioDictionary = new Dictionary<AudioType, List<Audio>>();
        private readonly Dictionary<AudioType, AudioPlayer> _playerDictionary = new Dictionary<AudioType, AudioPlayer>();

        private List<AudioPlayer> _activePlayer = new List<AudioPlayer>();

        public AudioPlayer voicePlayer;
        public List<AudioPlayer> effectSoundsPlayers;
        public List<Audio> audios;

        public void Awake()
        {
            Init(audios);
        }

        public bool IsPlaying()
        {
            foreach (var audioPlayer in effectSoundsPlayers)
            {
                AudioSource temp= audioPlayer.audioSource;
                if (!temp.loop)
                {
                    if (temp.isPlaying)
                    {
                        return true;
                    }
                }
            }

            if (!voicePlayer.audioSource.loop)
            {
                if (voicePlayer.audioSource.isPlaying)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsPlaying(AudioType type)
        {
            if (_playerDictionary.TryGetValue(type,out var player))
            {
                if (player!=null)
                {
                    return player.audioSource.isPlaying;
                }
            }

            if (voicePlayer.type ==type)
            {
                return voicePlayer.audioSource.isPlaying;
            }

            return false;
        }
        public void Init(List<Audio> audios)
        {

            foreach (var audio in audios)
            {
                audio.clip = ResourceManager.Instance.LoadResource<AudioClip>(audio.path);

                if (!_audioDictionary.ContainsKey(audio.type))
                {
                    _audioDictionary.Add(audio.type, new List<Audio>());
                }

                _audioDictionary[audio.type].Add(audio);
            }

            foreach (var player in effectSoundsPlayers)
            {
                AudioType type = player.type;
                if (_playerDictionary.ContainsKey(type))
                {
                    Debug.LogWarning("已存在相同类型的AudioPlayer,舍弃：" + type);
                }

                _playerDictionary.Add(type, player);
            }

        }

        public void StopPlay(AudioType type)
        {
            if (!_playerDictionary.TryGetValue(type, out var player))
            {
                //Debug.Log("找不到类型：" + type + " 的音频播放器");
                return;
            }

            player.audioSource.Stop();
        }

        public void PlayAudio(AudioType type, int index = 0)
        {
            if (_audioDictionary.TryGetValue(type, out var audios))
            {
                AudioClip clip = audios[index].clip;
                if (_playerDictionary.TryGetValue(type,out var player))
                {
                    player.audioSource.clip = clip;
                    if (Time.timeScale!=0)//暂停时不播音频
                    {
                        player.audioSource.Play();
                    }
                    return;
                }
                //Debug.Log("找不到类型：" + type + " 的音频播放器");
                return;
            }

            //Debug.Log("找不到类型：" + type + " 的音频");
        }

        public void PlayAudioRandom(AudioType type)
        {
            if (_audioDictionary.TryGetValue(type, out var audios))
            {
                int index = UnityEngine.Random.Range(0, audios.Count);
                PlayAudio(type, index);
            }

        }

        public void PlayVoice(AudioType type, int index = 0)
        {
            if (_audioDictionary.TryGetValue(type, out var audios))
            {
                AudioClip clip = audios[index].clip;
                voicePlayer.type = type;
                voicePlayer.audioSource.clip = clip;
                if (Time.timeScale !=0)
                {
                    voicePlayer.audioSource.Play();
                }
            }
        }

        public void PlayVoiceRandom(AudioType type)
        {
            if (_audioDictionary.TryGetValue(type, out var audios))
            {
                int index = UnityEngine.Random.Range(0, audios.Count);
                PlayVoice(type, index);
            }
        }
    }
}
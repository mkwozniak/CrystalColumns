using System;
using UnityObject = UnityEngine.Object;
using AudioSource = UnityEngine.AudioSource;
using AudioClip = UnityEngine.AudioClip;
using System.Diagnostics;

namespace Wozware.CrystalColumns
{
	[System.Serializable]
	public sealed class Audio
	{
		/// <summary> The primary asset library to use. </summary>
		public AssetLibrary Assets;

		/// <summary> The audio source for music. </summary>
		public AudioSource MusicSource;

		private int _fadeMusic = 0;
		private float _fadeMusicSpeed = 1.0f;
		private string _nextSong = "";
		private string _currentSong = "";
		private float _sfxVolume = 1.0f;
		private float _musicVolume = 1.0f;

		// events to other modules
		public Func<float> GetMusicVolume;
		public Func<float> GetSFXVolume;

		public void SetSFXVolume(float vol)
		{
			_sfxVolume = vol;
		}

		public void SetMusicVolume(float vol)
		{
			_musicVolume = vol;
		}

		public void ButtonHoverSFX()
		{
			SpawnSFX("button_hover");
		}

		public void SpawnSFX(string name)
		{
			if(!Assets.SFX.ContainsKey(name))
			{
				UnityEngine.Debug.Log($"Failed to spawn SFX {name}. No such SFX ID exists.");
				return;
			}

			AudioSource source = UnityObject.Instantiate(Assets.SoundEffectPrefab).GetComponent<AudioSource>();
			source.clip = Assets.SFX[name];
			source.volume = _sfxVolume;
			source.Play();
			UnityObject.Destroy(source.gameObject, source.clip.length);
		}

		public void SwitchMusic(string song)
		{
			if (song.Length <= 0)
			{
				MusicSource.Stop();
				return;
			}

			if (MusicSource.isPlaying)
			{
				_fadeMusic = -1;
				_nextSong = song;
			}
			else
			{
				MusicSource.volume = 0;
				_currentSong = song;
				MusicSource.clip = Assets.Music[_currentSong];
				MusicSource.Play();
				_fadeMusic = 1;
			}
		}

		public void StopMusic(bool fade = true)
		{
			if (!fade)
			{
				MusicSource.Stop();
				return;
			}

			if (MusicSource.isPlaying)
				_fadeMusic = -1;
		}

		public void UpdateMusicFading(float dt)
		{
			if (_fadeMusic != 0) // fading music in and out
			{
				MusicSource.volume += dt * _fadeMusicSpeed * _fadeMusic; // fade music up or down 

				if (MusicSource.volume <= 0 && _fadeMusic == -1) // music faded out
				{
					MusicSource.volume = 0;
					_fadeMusic = 0;
					if (_nextSong != _currentSong) // if its a switch
					{
						MusicSource.clip = Assets.Music[_nextSong]; // switch to next song and play
						MusicSource.Play();
						_fadeMusic = 1; // fade in music next
					}
				}
				else if (MusicSource.volume >= _musicVolume && _fadeMusic == 1) // music faded in
				{
					MusicSource.volume = _musicVolume;
					_fadeMusic = 0;
				}
			}
		}

		public void Update(float dt)
		{
			UpdateMusicFading(dt);
		}
	}
}


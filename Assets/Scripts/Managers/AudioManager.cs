using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

public class AudioManager : MonoBehaviour
{
    public enum AudioList
    {
        RightAnswer,
        WrongAnswer,
        GameOver,
        GameWin,
        Blocked,
        Attack,
        Click,
        CardColocation,
        Discard,
        Heal,
        Hurt,
        Bell,
        CrumpledPaper,
        PaperBreaking,
    }

    public enum MusicList
    {
        Menu,
        Game,
        Questions,
    }

    [SerializeField] private float fadeDuration;
    
    [SerializeField] private List<AudioClip> audioClips;
    [SerializeField] private List<AudioClip> musicList;

    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;

    [SerializeField] private AudioMixer mainMixer;

    private bool _masterMuted;
    private bool _sfxMuted;
    private bool _musicMuted;
    private MusicList _actualMusic;
    
    public void PlayAudio(AudioList clipName, float pitch = 1.0f, bool oneShoot = true)
    {
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(audioClips[(int)clipName]);
    }

    public void PlayMusic(MusicList songName, float pitch = 1.0f)
    {
        musicSource.pitch = pitch;
        musicSource.clip = musicList[(int)songName];
        musicSource.loop = true;
        musicSource.Play();
        _actualMusic = songName;
    }
    
    public void PlayMusicWithFade(MusicList songName, float pitch = 1.0f)
    {
        _actualMusic = songName;
        StartCoroutine(FadeMusic(musicList[(int)songName]));
    }
    
    private IEnumerator FadeMusic(AudioClip newClip)
    {
        float startVolume = musicSource.volume;

        // Fade out
        while (musicSource.volume > 0)
        {
            musicSource.volume -= startVolume * Time.deltaTime / fadeDuration;
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.loop = true;
        musicSource.Play();

        // Fade in
        while (musicSource.volume < startVolume)
        {
            musicSource.volume += startVolume * Time.deltaTime / fadeDuration;
            yield return null;
        }

        musicSource.volume = startVolume; // Asegurar que termine en el volumen exacto
    }

    public void PlayAudioPitched(AudioList clipName, bool oneShoot = true)
    {
        float randomPitch = sfxSource.pitch = Random.Range(0.9f, 1.1f);
        PlayAudio(clipName, randomPitch, oneShoot);
    }

    public void ToggleMusic()
    {
        PlayAudio(AudioList.Click);
        _musicMuted = !_musicMuted;
        SetMixerState("MusicVolume", _musicMuted);
    }

    public void ToggleSFX()
    {
        PlayAudio(AudioList.Click);
        _sfxMuted = !_sfxMuted;
        SetMixerState("SFXVolume", _sfxMuted);
    }

    public void SetMasterVolume(float volume)
    {
        mainMixer.SetFloat("MasterVolume", volume);
    }
    
    public void SetSFXVolume(float volume)
    {
        mainMixer.SetFloat("SFXVolume", volume);
        PlayAudio(AudioList.Click);
    }
    
    public void SetMusicVolume(float volume)
    {
        mainMixer.SetFloat("MusicVolume", volume);
    }

    public void ToggleMaster()
    {
        _masterMuted = !_masterMuted;
        SetMixerState("MasterVolume", _masterMuted);
    }

    private void SetMixerState(string parameter, bool muted)
    {
        mainMixer.SetFloat(parameter, (muted) ? -80.0f : 0.0f);
    }
    
    public MusicList GetActualMusic() => _actualMusic;
    public void StopMusic() => musicSource.Stop();
}

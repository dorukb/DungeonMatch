using System;
using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class AudioManager : MonoBehaviour {
    public static AudioManager Instance {get; private set;}

    [Header("Resources")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private SoundLibrary library;
    
    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup musicGroup;

    private List<AudioSource> _sfxPool;
    private AudioSource _musicSource;
    private const int POOL_SIZE = 25;
    
    // Tracks the last time a specific SoundType was played
    private Dictionary<SFXType, float> _lastPlayedTime = new Dictionary<SFXType, float>();
    // Minimum delay between the SAME sound (in seconds)
    [SerializeField] private float minInterval = 0.05f; 
    
    void Awake() {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        SetupMusicSource();
        SetupPool();
    }

    private void Start()
    {
        PlayMusic(MusicType.Intro);
    }

    private void SetupMusicSource() {
        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.outputAudioMixerGroup = musicGroup;
        _musicSource.loop = true;
    }

    private void SetupPool() {
        _sfxPool = new List<AudioSource>();
        for (int i = 0; i < POOL_SIZE; i++) {
            GameObject go = new GameObject($"SFX_Pool_{i}");
            go.transform.SetParent(transform);
            AudioSource source = go.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = sfxGroup;
            source.playOnAwake = false;
            _sfxPool.Add(source);
        }
    }

    public void PlaySFX(SFXType type, float pitchRange = 0.1f) {
        if (_lastPlayedTime.ContainsKey(type)) {
            if (Time.time - _lastPlayedTime[type] < minInterval) {
                return; // Skip this sound, it's playing too fast!
            }
        }
        AudioClip clip = library.GetRandomClip(type);
        if (clip == null) return;

        AudioSource source = GetAvailableSource();
        source.pitch = 1.0f + Random.Range(-pitchRange, pitchRange);
        source.PlayOneShot(clip);
    }

    public void PlayMusic(AudioClip musicClip) {
        if (_musicSource.clip == musicClip) return;
        _musicSource.clip = musicClip;
        _musicSource.Play();
    }
    
    public void PlayMusic(MusicType type, bool fade = true) {
        AudioClip clip = library.GetMusic(type);
        if (clip == null || _musicSource == null || _musicSource.clip == clip) return;

        // TODO: Use Tweener to cross-fade volume
        _musicSource.clip = clip;
        _musicSource.Play();
    }
    private AudioSource GetAvailableSource() {
        foreach (var source in _sfxPool) {
            if (!source.isPlaying) return source;
        }
        return _sfxPool[0]; // Overwrite oldest if full
    }

    public void ToggleMute(bool isMuted) {
        // -80dB is silence in Unity Mixer
        mixer.SetFloat("SFXVol", isMuted ? -80f : 0f);
        mixer.SetFloat("MusicVol", isMuted ? -80f : 0f);
    }

    public void SetGroupVolume(string parameterName, float sliderValue) {
        // sliderValue should be 0.0001 to 1.0
        // converts linear slider to logarithmic decibels
        float dB = Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20;
        mixer.SetFloat(parameterName, dB);
    }
}
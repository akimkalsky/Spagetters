using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    AudioSource music, sfx, ambient;
    readonly Dictionary<string, AudioClip> cache = new();

    float sfxScale = 1f, musicScale = 1f, musicBase = 0.5f, ambientBase = 0.35f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        music = gameObject.AddComponent<AudioSource>();
        music.loop = true; music.playOnAwake = false;

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;

        ambient = gameObject.AddComponent<AudioSource>();
        ambient.loop = true; ambient.playOnAwake = false;
    }

    AudioClip Load(string name)
    {
        if (cache.TryGetValue(name, out var c))
        {
            return c;
        }
        c = Resources.Load<AudioClip>("Audio/" + name) ?? ProceduralSfx.Build(name);
        cache[name] = c;
        return c;
    }

    public void PlaySfx(string name, float volume = 1f)
    {
        var clip = Load(name);
        if (clip != null)
        {
            sfx.PlayOneShot(clip, volume * sfxScale);
        }
    }

    public void PlayMusic(string name, float volume = 0.5f)
    {
        var clip = Load(name);
        if (clip == null)
        {
            return;
        }
        musicBase = volume;
        music.clip = clip; music.volume = volume * musicScale; music.Play();
    }

    public void PlayAmbient(string name, float volume = 0.35f)
    {
        var clip = Load(name);
        if (clip == null)
        {
            return;
        }
        ambientBase = volume;
        ambient.clip = clip; ambient.volume = volume * musicScale; ambient.Play();
    }

    public void StopAmbient() => ambient.Stop();
    public void StopMusic() => music.Stop();

    public void SetMasterVolume(float v) => AudioListener.volume = v;
    public void SetSfxVolume(float v) => sfxScale = v;

    public void SetMusicVolume(float v)
    {
        musicScale = v;
        music.volume = musicBase * v;
        ambient.volume = ambientBase * v;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    AudioSource musicA, musicB, sfx, ambient;
    AudioSource activeMusic;
    Coroutine musicFade;
    readonly Dictionary<string, AudioClip> cache = new();

    float sfxScale = 1f, musicScale = 1f, musicBase = 0.5f, ambientBase = 0.35f;
    string currentMusic, currentAmbient;

    const float MusicFadeSeconds = 1.2f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        foreach (var l in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
        {
            if (l.gameObject != gameObject)
            {
                Destroy(l);
            }
        }
        if (GetComponent<AudioListener>() == null)
        {
            gameObject.AddComponent<AudioListener>();
        }

        musicA = gameObject.AddComponent<AudioSource>();
        musicA.loop = true; musicA.playOnAwake = false;

        musicB = gameObject.AddComponent<AudioSource>();
        musicB.loop = true; musicB.playOnAwake = false;

        activeMusic = musicA;

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

    public void PlaySfx(string name, float volume = 1f) => PlaySfx(name, volume, 0f);

    public void PlaySfx(string name, float volume, float pan)
    {
        var clip = Load(name);
        if (clip != null)
        {
            sfx.panStereo = pan;
            sfx.PlayOneShot(clip, volume * sfxScale);
        }
    }

    public void PlayMusic(string name, float volume = 0.5f) => PlayMusic(name, volume, MusicFadeSeconds);

    public void PlayMusic(string name, float volume, float fadeSeconds)
    {
        if (name == currentMusic && activeMusic.isPlaying)
        {
            return;
        }
        var clip = Load(name);
        if (clip == null)
        {
            return;
        }
        currentMusic = name;
        musicBase = volume;

        var next = activeMusic == musicA ? musicB : musicA;
        next.clip = clip;
        next.volume = 0f;
        next.Play();

        var prev = activeMusic;
        activeMusic = next;

        if (musicFade != null)
        {
            StopCoroutine(musicFade);
        }
        musicFade = StartCoroutine(Crossfade(prev, next, fadeSeconds));
    }

    IEnumerator Crossfade(AudioSource from, AudioSource to, float dur)
    {
        float fromStart = from.volume;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            to.volume = musicBase * musicScale * k;
            from.volume = fromStart * (1f - k);
            yield return null;
        }
        to.volume = musicBase * musicScale;
        from.volume = 0f;
        from.Stop();
        from.clip = null;
        musicFade = null;
    }

    public void FadeOutMusic(float fadeSeconds = 0.8f)
    {
        currentMusic = null;
        if (musicFade != null)
        {
            StopCoroutine(musicFade);
        }
        musicFade = StartCoroutine(FadeOut(activeMusic, fadeSeconds));
    }

    IEnumerator FadeOut(AudioSource src, float dur)
    {
        float start = src.volume;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(start, 0f, t / dur);
            yield return null;
        }
        src.volume = 0f;
        src.Stop();
        src.clip = null;
        musicFade = null;
    }

    public void PlayAmbient(string name, float volume = 0.35f)
    {
        ambientBase = volume;
        if (name == currentAmbient && ambient.isPlaying)
        {
            ambient.volume = volume * musicScale;
            return;
        }
        var clip = Load(name);
        if (clip == null)
        {
            return;
        }
        currentAmbient = name;
        ambient.clip = clip; ambient.volume = volume * musicScale; ambient.Play();
    }

    public void StopAmbient()
    {
        ambient.Stop();
        currentAmbient = null;
    }

    public void StopMusic()
    {
        if (musicFade != null)
        {
            StopCoroutine(musicFade);
            musicFade = null;
        }
        musicA.Stop();
        musicB.Stop();
        currentMusic = null;
    }

    public void SetMasterVolume(float v) => AudioListener.volume = v;
    public void SetSfxVolume(float v) => sfxScale = v;

    public void SetMusicVolume(float v)
    {
        musicScale = v;
        activeMusic.volume = musicBase * v;
        ambient.volume = ambientBase * v;
    }
}

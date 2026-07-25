using UnityEngine;

public static class ProceduralSfx
{
    const int SR = 44100;
    static readonly System.Random rng = new System.Random(9001);

    public enum Wave { Sine, Square, Saw, Triangle }

    public static AudioClip Build(string name)
    {
        switch (name)
        {
            case "ui_move":    return Blip(620f, 0.05f, 0.18f, Wave.Triangle, 780f);
            case "ui_confirm": return Blip(720f, 0.11f, 0.22f, Wave.Triangle, 1050f);
            case "ui_back":    return Blip(520f, 0.11f, 0.2f, Wave.Triangle, 320f);
            case "tick":       return Blip(1000f, 0.03f, 0.12f, Wave.Triangle, 1000f);
            case "footstep":   return Footstep();
            case "draw":       return DrawSting();
            case "rival":      return Blip(210f, 0.5f, 0.4f, Wave.Saw, 95f);
            case "coin":       return Blip(1200f, 0.12f, 0.3f, Wave.Triangle, 1900f);
            case "explosion":  return Explosion();
            case "shatter":    return Shatter();
            case "empty":      return Blip(1300f, 0.03f, 0.11f, Wave.Triangle, 1200f);
            case "clunk":      return Blip(150f, 0.14f, 0.45f, Wave.Triangle, 90f);
            case "alarm":      return Blip(720f, 0.22f, 0.28f, Wave.Triangle, 720f);
            case "type":       return Blip(950f, 0.02f, 0.06f, Wave.Triangle, 950f);
            case "gunshot":    return Gunshot();
            case "win":        return Chord(new[] { 523.25f, 659.25f, 783.99f }, 0.7f, false);
            case "lose":       return Chord(new[] { 349.23f, 261.63f, 196f }, 0.9f, true);
            case "wind":       return Wind(4f);
            default:           return null;
        }
    }

    static AudioClip FromSamples(string name, float[] data, bool loop) => FromSamples(name, data, loop, 0.45f);

    static AudioClip FromSamples(string name, float[] data, bool loop, float lowpass)
    {
        if (lowpass < 0.999f)
        {
            Lowpass(data, lowpass);
        }
        var clip = AudioClip.Create(name, data.Length, 1, SR, false);
        clip.SetData(data, 0);
        return clip;
    }

    static void Lowpass(float[] d, float a)
    {
        float y = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            y += a * (d[i] - y);
            d[i] = y;
        }
    }

    static float Osc(Wave w, float phase)
    {
        switch (w)
        {
            case Wave.Square:   return phase < 0.5f ? 1f : -1f;
            case Wave.Saw:      return 2f * phase - 1f;
            case Wave.Triangle: return 4f * Mathf.Abs(phase - 0.5f) - 1f;
            default:            return Mathf.Sin(phase * 2f * Mathf.PI);
        }
    }

    static float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);

    static void Normalize(float[] d, float peak = 0.9f)
    {
        float max = 0f;
        for (int i = 0; i < d.Length; i++) max = Mathf.Max(max, Mathf.Abs(d[i]));
        if (max < 1e-5f)
        {
            return;
        }
        float g = peak / max;
        for (int i = 0; i < d.Length; i++) d[i] *= g;
    }

    static AudioClip Blip(float freq, float dur, float vol, Wave w, float sweepTo)
    {
        int n = Mathf.CeilToInt(SR * dur);
        var d = new float[n];
        float phase = 0f;
        int atk = Mathf.Max(1, (int)(SR * 0.002f));
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float f = Mathf.Lerp(freq, sweepTo, t);
            phase += f / SR;
            if (phase >= 1f)
            {
                phase -= 1f;
            }
            float env = Mathf.Exp(-5f * t) * Mathf.Min(1f, (float)i / atk);
            d[i] = Osc(w, phase) * env * vol;
        }
        return FromSamples("blip", d, false);
    }

    static AudioClip Footstep()
    {
        int n = (int)(SR * 0.12f);
        var d = new float[n];
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            lp += 0.10f * (Noise() - lp);
            d[i] = lp * Mathf.Exp(-14f * t) * 0.9f;
        }
        Normalize(d, 0.7f);
        return FromSamples("footstep", d, false);
    }

    static AudioClip DrawSting()
    {
        int n = (int)(SR * 0.26f);
        var d = new float[n];
        float ph = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float f = Mathf.Lerp(760f, 2000f, Mathf.Min(1f, t * 6f));
            ph += f / SR;
            if (ph >= 1f)
            {
                ph -= 1f;
            }
            float tone = Mathf.Sin(ph * 2f * Mathf.PI) + Osc(Wave.Square, ph) * 0.22f;
            float env = Mathf.Exp(-9f * t) * Mathf.Min(1f, i / 24f);
            float spit = Noise() * Mathf.Exp(-34f * t) * 0.3f;
            d[i] = tone * env * 0.8f + spit;
        }
        Normalize(d, 0.92f);
        return FromSamples("draw", d, false, 0.72f);
    }

    static AudioClip Gunshot()
    {
        int n = (int)(SR * 0.6f);
        var d = new float[n];
        float lp = 0f, bodyPh = 0f, subPh = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;

            float click = i < 45 ? (1f - i / 45f) : 0f;

            lp += 0.8f * (Noise() - lp);
            float crack = lp * Mathf.Exp(-30f * t);

            float blast = Noise() * Mathf.Exp(-11f * t) * 0.6f;

            float bodyFreq = Mathf.Lerp(220f, 45f, Mathf.Min(1f, t * 3.5f));
            bodyPh += bodyFreq / SR;
            if (bodyPh >= 1f)
            {
                bodyPh -= 1f;
            }
            float body = Mathf.Sin(bodyPh * 2f * Mathf.PI) * Mathf.Exp(-6.5f * t);

            subPh += 42f / SR;
            if (subPh >= 1f)
            {
                subPh -= 1f;
            }
            float sub = Mathf.Sin(subPh * 2f * Mathf.PI) * Mathf.Exp(-5f * t) * 0.75f;

            float x = (click * 1.0f + crack * 0.9f + blast * 0.5f + body * 1.1f + sub * 0.85f) * 1.7f;
            d[i] = x / (1f + Mathf.Abs(x));
        }
        Normalize(d, 1.0f);
        return FromSamples("gunshot", d, false, 0.9f);
    }

    static AudioClip Explosion()
    {
        int n = (int)(SR * 0.9f);
        var d = new float[n];
        float lp = 0f, ph = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            lp += 0.25f * (Noise() - lp);
            float rumble = lp * Mathf.Exp(-4f * t);
            ph += 55f / SR;
            if (ph >= 1f)
            {
                ph -= 1f;
            }
            float boom = Mathf.Sin(ph * 2f * Mathf.PI) * Mathf.Exp(-3f * t);
            d[i] = rumble * 0.7f + boom * 0.7f;
        }
        Normalize(d, 0.98f);
        return FromSamples("explosion", d, false);
    }

    static AudioClip Shatter()
    {
        int n = (int)(SR * 0.22f);
        var d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float noise = Noise() * Mathf.Exp(-28f * t);
            float ping = (Mathf.Sin(2f * Mathf.PI * 3200f * i / SR) * 0.3f
                        + Mathf.Sin(2f * Mathf.PI * 4700f * i / SR) * 0.2f) * Mathf.Exp(-18f * t);
            d[i] = noise * 0.7f + ping;
        }
        Normalize(d, 0.8f);
        return FromSamples("shatter", d, false);
    }

    static AudioClip Chord(float[] freqs, float dur, bool minorFall)
    {
        int n = (int)(SR * dur);
        var d = new float[n];
        var phase = new float[freqs.Length];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float env = Mathf.Min(1f, t * 12f) * Mathf.Exp(-2.5f * t);
            float s = 0f;
            for (int k = 0; k < freqs.Length; k++)
            {
                float f = freqs[k] * (minorFall ? Mathf.Lerp(1f, 0.985f, t) : 1f);
                phase[k] += f / SR;
                if (phase[k] >= 1f)
                {
                    phase[k] -= 1f;
                }
                s += Mathf.Sin(phase[k] * 2f * Mathf.PI);
            }
            d[i] = s / freqs.Length * env;
        }
        Normalize(d, 0.8f);
        return FromSamples("chord", d, false);
    }

    static AudioClip Wind(float dur)
    {
        int n = (int)(SR * dur);
        var d = new float[n];
        float brown = 0f, lp = 0f;
        for (int i = 0; i < n; i++)
        {
            brown += Noise() * 0.02f;
            brown = Mathf.Clamp(brown, -1f, 1f);
            lp += 0.05f * (brown - lp);
            float lfo = 0.55f + 0.45f * Mathf.Sin((float)i / n * 2f * Mathf.PI * 2f);
            d[i] = lp * lfo;
        }
        int x = (int)(SR * 0.25f);
        for (int i = 0; i < x; i++)
        {
            float a = (float)i / x;
            d[i] = Mathf.Lerp(d[n - x + i], d[i], a);
        }
        Normalize(d, 0.5f);
        return FromSamples("wind", d, true);
    }
}

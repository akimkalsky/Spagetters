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
            case "death":      return Death();
            case "win":        return Chord(new[] { 523.25f, 659.25f, 783.99f }, 0.7f, false);
            case "lose":       return Chord(new[] { 349.23f, 261.63f, 196f }, 0.9f, true);
            case "wind":       return Wind(4f);
            case "coward":     return Coward();
            case "streak":     return Streak();
            case "feint":      return Feint();
            case "caw":        return Caw();
            case "bell":       return Bell();
            case "whoosh":     return Whoosh();
            case "fail":       return Fail();
            case "riser":      return Riser();
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
        int n = (int)(SR * 1.0f);
        var d = new float[n];
        float lp = 0f, prevNoise = 0f, bodyPh = 0f, subPh = 0f;
        float ring1 = 0f, ring2 = 0f, ring3 = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float ms = i * 1000f / SR;

            float nz = Noise();
            float crack = (nz - prevNoise) * Mathf.Exp(-ms / 3f);
            prevNoise = nz;

            lp += 0.55f * (nz - lp);
            float blast = lp * Mathf.Exp(-ms / 16f);

            bodyPh = Advance(bodyPh, Mathf.Lerp(240f, 52f, Mathf.Min(1f, t * 6f)));
            float body = Mathf.Sin(bodyPh * 2f * Mathf.PI) * Mathf.Exp(-ms / 50f);

            subPh = Advance(subPh, Mathf.Lerp(58f, 34f, Mathf.Min(1f, t * 8f)));
            float sub = Mathf.Sin(subPh * 2f * Mathf.PI) * Mathf.Exp(-ms / 110f);

            ring1 = Advance(ring1, 1900f);
            ring2 = Advance(ring2, 2840f);
            ring3 = Advance(ring3, 4130f);
            float ping = (Mathf.Sin(ring1 * 2f * Mathf.PI)
                          + Mathf.Sin(ring2 * 2f * Mathf.PI) * 0.6f
                          + Mathf.Sin(ring3 * 2f * Mathf.PI) * 0.35f) * Mathf.Exp(-ms / 55f);

            d[i] = crack * 2.2f + blast * 1.1f + body * 0.95f + sub * 1.05f + ping * 0.55f;
        }

        Zing(d, 0.045f, 0.20f, 2850f, 820f, 0.62f, 0.75f);
        Zing(d, 0.215f, 0.24f, 2050f, 600f, 0.34f, 0.85f);
        Zing(d, 0.430f, 0.26f, 1500f, 470f, 0.17f, 0.95f);

        SlapBack(d, 0.115f, 0.30f);
        SlapBack(d, 0.255f, 0.15f);
        Saturate(d, 1.35f);
        FadeTail(d, 0.15f);
        Normalize(d, 1.0f);
        return FromSamples("gunshot", d, false, 0.98f);
    }

    static void Zing(float[] d, float startSeconds, float durSeconds, float fromHz, float toHz, float gain, float bass)
    {
        int start = (int)(SR * startSeconds);
        int len = (int)(SR * durSeconds);
        if (start >= d.Length || len <= 0)
        {
            return;
        }
        float ph = 0f, low = 0f, vib = 0f;
        for (int i = 0; i < len && start + i < d.Length; i++)
        {
            float u = (float)i / len;
            float f = Mathf.Lerp(fromHz, toHz, u * u);
            vib = Advance(vib, 38f);
            float wobble = 1f + 0.012f * Mathf.Sin(vib * 2f * Mathf.PI);
            ph = Advance(ph, f * wobble);
            low = Advance(low, f * 0.25f * wobble);
            float tone = Mathf.Sin(ph * 2f * Mathf.PI) + Osc(Wave.Triangle, ph) * 0.18f;
            float under = Mathf.Sin(low * 2f * Mathf.PI) * bass;
            float env = Mathf.Min(1f, i / 90f) * Mathf.Exp(-3.2f * u);
            d[start + i] += (tone + under) * env * gain;
        }
    }

    static float Advance(float phase, float hz)
    {
        phase += hz / SR;
        return phase >= 1f ? phase - 1f : phase;
    }

    static AudioClip Death()
    {
        int n = (int)(SR * 1.8f);
        var d = new float[n];
        float lp = 0f, ph = 0f, third = 0f, wob = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float ms = i * 1000f / SR;

            lp += 0.12f * (Noise() - lp);
            float thud = lp * Mathf.Exp(-ms / 55f) * 1.6f;

            float f = Mathf.Lerp(190f, 52f, Mathf.Pow(t, 0.55f));
            ph += f / SR;
            if (ph >= 1f)
            {
                ph -= 1f;
            }
            float groan = (Mathf.Sin(ph * 2f * Mathf.PI) + Osc(Wave.Triangle, ph) * 0.35f) * Mathf.Exp(-2.1f * t);

            third += f * 1.19f / SR;
            if (third >= 1f)
            {
                third -= 1f;
            }
            float partial = Mathf.Sin(third * 2f * Mathf.PI) * Mathf.Exp(-3.4f * t) * 0.4f;

            wob += 3.5f / SR;
            if (wob >= 1f)
            {
                wob -= 1f;
            }
            float tremolo = 0.85f + 0.15f * Mathf.Sin(wob * 2f * Mathf.PI);

            d[i] = (thud + (groan + partial) * tremolo * 0.75f) * Mathf.Min(1f, i / 60f);
        }

        SlapBack(d, 0.19f, 0.22f);
        FadeTail(d, 0.25f);
        Normalize(d, 0.95f);
        return FromSamples("death", d, false, 0.5f);
    }

    static void SlapBack(float[] d, float delaySeconds, float gain)
    {
        int delay = (int)(SR * delaySeconds);
        if (delay <= 0 || delay >= d.Length)
        {
            return;
        }
        var echo = new float[d.Length];
        float y = 0f;
        for (int i = delay; i < d.Length; i++)
        {
            y += 0.22f * (d[i - delay] - y);
            echo[i] = y * gain;
        }
        for (int i = 0; i < d.Length; i++)
        {
            d[i] += echo[i];
        }
    }

    static void FadeTail(float[] d, float seconds)
    {
        int fade = Mathf.Min(d.Length, (int)(SR * seconds));
        if (fade <= 0)
        {
            return;
        }
        int start = d.Length - fade;
        for (int i = start; i < d.Length; i++)
        {
            d[i] *= 1f - (float)(i - start) / fade;
        }
    }

    static void Saturate(float[] d, float drive)
    {
        for (int i = 0; i < d.Length; i++)
        {
            float x = d[i] * drive;
            d[i] = x / (1f + Mathf.Abs(x));
        }
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

    static AudioClip Coward()
    {
        int n = (int)(SR * 0.55f);
        var d = new float[n];
        float ph = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float f = Mathf.Lerp(1400f, 320f, t * t);
            f *= 1f + 0.03f * Mathf.Sin(t * 40f);
            ph += f / SR;
            if (ph >= 1f)
            {
                ph -= 1f;
            }
            float env = Mathf.Min(1f, i / 400f) * Mathf.Exp(-1.4f * t);
            d[i] = Osc(Wave.Triangle, ph) * env * 0.7f;
        }
        Normalize(d, 0.7f);
        return FromSamples("coward", d, false, 0.9f);
    }

    static AudioClip Streak()
    {
        float[] notes = { 784f, 988f, 1319f };
        int per = (int)(SR * 0.09f);
        int n = per * notes.Length + (int)(SR * 0.16f);
        var d = new float[n];
        for (int k = 0; k < notes.Length; k++)
        {
            float ph = 0f;
            int start = k * per;
            int len = (int)(SR * 0.2f);
            for (int j = 0; j < len && start + j < n; j++)
            {
                float t = (float)j / len;
                ph += notes[k] / SR;
                if (ph >= 1f)
                {
                    ph -= 1f;
                }
                float env = Mathf.Exp(-6f * t) * Mathf.Min(1f, j / 20f);
                d[start + j] += (Osc(Wave.Triangle, ph) + Osc(Wave.Square, ph) * 0.2f) * env * 0.5f;
            }
        }
        Normalize(d, 0.8f);
        return FromSamples("streak", d, false, 1f);
    }

    static AudioClip Feint()
    {
        int n = (int)(SR * 0.09f);
        var d = new float[n];
        float ph = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float f = Mathf.Lerp(500f, 360f, t);
            ph += f / SR;
            if (ph >= 1f)
            {
                ph -= 1f;
            }
            float env = Mathf.Exp(-10f * t) * Mathf.Min(1f, i / 30f);
            d[i] = Osc(Wave.Triangle, ph) * env * 0.4f;
        }
        return FromSamples("feint", d, false, 0.4f);
    }

    static AudioClip Caw()
    {
        int n = (int)(SR * 0.5f);
        var d = new float[n];
        float ph = 0f, lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float f = Mathf.Lerp(900f, 520f, Mathf.Min(1f, t * 1.5f));
            ph += f / SR;
            if (ph >= 1f)
            {
                ph -= 1f;
            }
            lp += 0.5f * (Noise() - lp);
            float gate = 0.6f + 0.4f * Mathf.Sin(t * 30f);
            float env = Mathf.Min(1f, i / 200f) * Mathf.Exp(-3f * t);
            d[i] = (Osc(Wave.Saw, ph) * 0.7f + lp * 0.4f) * gate * env * 0.7f;
        }
        Normalize(d, 0.75f);
        return FromSamples("caw", d, false, 0.6f);
    }

    static AudioClip Bell()
    {
        int n = (int)(SR * 2.2f);
        var d = new float[n];
        float fund = 293f;
        float[] ratios = { 0.5f, 1f, 1.19f, 1.56f, 2f, 2.66f, 3.0f };
        float[] amps = { 0.4f, 1f, 0.6f, 0.5f, 0.4f, 0.25f, 0.2f };
        float[] decay = { 1.2f, 1.6f, 2.2f, 2.8f, 3.2f, 4f, 4.5f };
        var ph = new float[ratios.Length];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR;
            float s = 0f;
            for (int k = 0; k < ratios.Length; k++)
            {
                ph[k] += fund * ratios[k] / SR;
                if (ph[k] >= 1f)
                {
                    ph[k] -= 1f;
                }
                s += Mathf.Sin(ph[k] * 2f * Mathf.PI) * amps[k] * Mathf.Exp(-decay[k] * t);
            }
            d[i] = s * Mathf.Min(1f, i / 40f) * 0.3f;
        }
        Normalize(d, 0.85f);
        return FromSamples("bell", d, false, 1f);
    }

    static AudioClip Whoosh()
    {
        int n = (int)(SR * 0.7f);
        var d = new float[n];
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float a = Mathf.Lerp(0.25f, 0.02f, t);
            lp += a * (Noise() - lp);
            d[i] = lp * Mathf.Sin(Mathf.PI * t) * 1.2f;
        }
        Normalize(d, 0.7f);
        return FromSamples("whoosh", d, false, 1f);
    }

    static AudioClip Fail()
    {
        float[] notes = { 330f, 294f, 262f, 196f };
        int per = (int)(SR * 0.14f);
        int n = per * notes.Length + (int)(SR * 0.12f);
        var d = new float[n];
        for (int k = 0; k < notes.Length; k++)
        {
            float ph = 0f;
            int start = k * per;
            int len = (int)(SR * 0.22f);
            for (int j = 0; j < len && start + j < n; j++)
            {
                float t = (float)j / len;
                float f = notes[k] * Mathf.Lerp(1f, 0.94f, t);
                ph += f / SR;
                if (ph >= 1f)
                {
                    ph -= 1f;
                }
                float env = Mathf.Min(1f, j / 40f) * Mathf.Exp(-3.5f * t);
                d[start + j] += (Osc(Wave.Saw, ph) * 0.6f + Osc(Wave.Triangle, ph) * 0.4f) * env * 0.5f;
            }
        }
        Normalize(d, 0.8f);
        return FromSamples("fail", d, false, 0.7f);
    }

    static AudioClip Riser()
    {
        int n = (int)(SR * 1.6f);
        var d = new float[n];
        float ph = 0f, lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float f = Mathf.Lerp(110f, 440f, t * t);
            ph += f / SR;
            if (ph >= 1f)
            {
                ph -= 1f;
            }
            lp += 0.03f * (Noise() - lp);
            float env = Mathf.Min(1f, t * 3f) * Mathf.Min(1f, (1f - t) * 6f);
            d[i] = (Osc(Wave.Saw, ph) * 0.5f + lp * 0.5f) * env * 0.6f;
        }
        Normalize(d, 0.65f);
        return FromSamples("riser", d, false, 0.5f);
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

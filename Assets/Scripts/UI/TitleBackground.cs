using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TitleBackground : MonoBehaviour
{
    const float RefW = 1920f, RefH = 1080f, Horizon = -40f;

    Canvas canvas;
    Image nightSky, sun;
    RectTransform sunRt;
    Vector2 sunDay = new Vector2(-320, 60);
    readonly List<(RectTransform rt, Vector2 basePos, float factor)> layers = new();
    readonly List<Image> nightTint = new();

    void Start()
    {
        Build();
        GameFlow.Instance.StateChanged += OnState;
        OnState(GameFlow.Instance.State);
    }

    void OnDestroy()
    {
        if (GameFlow.Instance != null)
        {
            GameFlow.Instance.StateChanged -= OnState;
        }
    }

    void OnState(GameState s) => canvas.gameObject.SetActive(s == GameState.MainMenu);

    void Build()
    {
        canvas = UIFactory.CreateOverlayCanvas("TitleBgCanvas", 90, transform);

        var day = new Color(0.98f, 0.80f, 0.52f);
        Sky(day, new Color(0.85f, 0.45f, 0.22f), new Color(0.16f, 0.13f, 0.28f), 1f);
        nightSky = Sky(new Color(0.10f, 0.12f, 0.22f), new Color(0.05f, 0.06f, 0.14f), new Color(0.02f, 0.02f, 0.06f), 0f);

        sun = Layer(SunTex(256), sunDay, new Vector2(340, 340), new Vector2(0.5f, 0.5f), 0f);
        sunRt = sun.rectTransform;

        nightTint.Add(Mesa(new Color(0.45f, 0.35f, 0.42f), 260f, 0.30f, 11));
        nightTint.Add(Mesa(new Color(0.32f, 0.22f, 0.28f), 360f, 0.60f, 27));
        nightTint.Add(Mesa(new Color(0.18f, 0.11f, 0.13f), 470f, 1.00f, 43));
        nightTint.Add(Solid(new Color(0.12f, 0.08f, 0.06f), new Vector2(0, Horizon - 600), new Vector2(2400, 1200), 0.20f));
    }

    Image Sky(Color horizon, Color mid, Color top, float alpha)
    {
        var img = Layer(SkyTex(256, horizon, mid, top), Vector2.zero, new Vector2(RefW, RefH), Vector2.zero, 0f, true);
        var c = Color.white; c.a = alpha; img.color = c;
        return img;
    }

    Image Mesa(Color col, float height, float factor, int seed)
    {
        return Layer(MesaTex(512, 256, col, seed), new Vector2(0, Horizon),
                     new Vector2(2400, height), new Vector2(0.5f, 0f), factor);
    }

    Image Solid(Color col, Vector2 pos, Vector2 size, float factor)
    {
        var img = NewImage("Ground");
        img.color = col;
        Setup(img.rectTransform, pos, size, new Vector2(0.5f, 0.5f), factor, false, addToLayers: true);
        return img;
    }

    Image Layer(Sprite sp, Vector2 pos, Vector2 size, Vector2 pivot, float factor, bool stretch = false)
    {
        var img = NewImage(sp.name);
        img.sprite = sp;
        Setup(img.rectTransform, pos, size, pivot, factor, stretch, addToLayers: sp.name != "sun");
        return img;
    }

    Image NewImage(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(canvas.transform, false);
        return go.AddComponent<Image>();
    }

    void Setup(RectTransform rt, Vector2 pos, Vector2 size, Vector2 pivot, float factor, bool stretch, bool addToLayers)
    {
        if (stretch)
        {
            UIFactory.Stretch(rt);
        }
        else
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }
        if (addToLayers)
        {
            layers.Add((rt, pos, factor));
        }
    }

    void Update()
    {
        var m = Mouse.current;
        Vector2 mp = m != null ? m.position.ReadValue() : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 n = new Vector2(mp.x / Screen.width - 0.5f, mp.y / Screen.height - 0.5f) * 2f;

        float drift = Mathf.Sin(Time.unscaledTime * 0.1f);
        foreach (var l in layers)
        {
            if (l.factor <= 0f)
            {
                continue;
            }
            var off = new Vector2(n.x * l.factor * 45f + drift * l.factor * 8f, n.y * l.factor * 22f);
            l.rt.anchoredPosition = l.basePos + off;
        }

        float night = DayNightClock.Instance != null ? DayNightClock.Instance.Night : 0f;
        ApplyNight(night, n);
    }

    void ApplyNight(float night, Vector2 mouse)
    {
        var sc = nightSky.color; sc.a = night; nightSky.color = sc;

        var pos = Vector2.Lerp(sunDay, new Vector2(sunDay.x, -280f), night);
        pos += new Vector2(mouse.x * 0.15f * 45f, mouse.y * 0.15f * 22f);
        sunRt.anchoredPosition = pos;
        var sunCol = Color.Lerp(new Color(1f, 0.93f, 0.7f), new Color(0.7f, 0.15f, 0.05f), night);
        sunCol.a = Mathf.Clamp01(1.2f - night * 1.5f);
        sun.color = sunCol;

        var tint = Color.Lerp(Color.white, new Color(0.38f, 0.4f, 0.55f), night);
        foreach (var img in nightTint) img.color = tint;
    }

    static Sprite SkyTex(int h, Color horizon, Color mid, Color top)
    {
        return ProceduralTex.Generate(1, h, (x, y) =>
        {
            float f = (float)y / (h - 1);
            return f < 0.5f ? Color.Lerp(horizon, mid, f * 2f) : Color.Lerp(mid, top, (f - 0.5f) * 2f);
        });
    }

    static Sprite SunTex(int d)
    {
        return ProceduralTex.Disc(d, Color.white);
    }

    static Sprite MesaTex(int w, int h, Color col, int seed)
    {
        var rnd = new System.Random(seed);
        int baseH = h / 6;
        var height = new int[w];
        for (int x = 0; x < w; x++)
        {
            height[x] = baseH;
        }

        int plateaus = 6 + rnd.Next(4);
        for (int p = 0; p < plateaus; p++)
        {
            int cw = w / 12 + rnd.Next(w / 8);
            int cx = rnd.Next(w);
            int ph = baseH + rnd.Next(h / 2);
            int slope = Mathf.Max(4, cw / 8);
            for (int x = cx - cw / 2 - slope; x <= cx + cw / 2 + slope; x++)
            {
                if (x < 0 || x >= w)
                {
                    continue;
                }
                int d = Mathf.Abs(x - cx) - cw / 2;
                int hh = d <= 0 ? ph : ph - d * (ph - baseH) / slope;
                height[x] = Mathf.Max(height[x], Mathf.Max(hh, baseH));
            }
        }

        return ProceduralTex.Generate(w, h, (x, y) => y < Mathf.Clamp(height[x], 0, h) ? col : Color.clear);
    }
}

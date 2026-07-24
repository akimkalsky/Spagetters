using System;
using UnityEngine;

public static class ProceduralTex
{
    public static Sprite Generate(int w, int h, Func<int, int, Color> shader)
    {
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                px[y * w + x] = shader(x, y);
            }
        }
        return FromPixels(px, w, h);
    }

    public static Sprite FromPixels(Color[] px, int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    public static Sprite Disc(int size, Color color)
    {
        float r = size * 0.5f;
        var c = new Vector2(r, r);
        return Generate(size, size, (x, y) =>
            Vector2.Distance(new Vector2(x, y), c) <= r ? color : Color.clear);
    }

    public static Sprite SoftDisc(int size, Color color)
    {
        float r = size * 0.5f;
        var c = new Vector2(r, r);
        return Generate(size, size, (x, y) =>
        {
            float a = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), c) / r);
            var col = color;
            col.a *= a * a;
            return col;
        });
    }

    public static Sprite Star(int size, Color color)
    {
        int points = 5;
        float cx = size * 0.5f, outer = size * 0.48f, inner = size * 0.20f;
        var v = new Vector2[points * 2];
        for (int i = 0; i < v.Length; i++)
        {
            float ang = Mathf.PI / 2f + i * Mathf.PI / points;
            float rad = i % 2 == 0 ? outer : inner;
            v[i] = new Vector2(cx + Mathf.Cos(ang) * rad, cx + Mathf.Sin(ang) * rad);
        }
        return Generate(size, size, (x, y) => InPoly(v, x + 0.5f, y + 0.5f) ? color : Color.clear);
    }

    public static Sprite TriangleDown(int size, Color color)
    {
        return Generate(size, size, (x, y) =>
        {
            float half = (float)y / size * 0.5f;
            return Mathf.Abs((float)x / size - 0.5f) <= half ? color : Color.clear;
        });
    }

    static bool InPoly(Vector2[] v, float px, float py)
    {
        bool inside = false;
        for (int i = 0, j = v.Length - 1; i < v.Length; j = i++)
        {
            if (v[i].y > py != v[j].y > py &&
                px < (v[j].x - v[i].x) * (py - v[i].y) / (v[j].y - v[i].y) + v[i].x)
            {
                inside = !inside;
            }
        }
        return inside;
    }
}

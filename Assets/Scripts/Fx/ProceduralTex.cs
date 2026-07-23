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
}

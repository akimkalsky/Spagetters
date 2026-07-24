using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DuelToastController : MonoBehaviour
{
    Canvas canvas;
    TMP_Text headline, stats, line;
    Image barTrack, barFill;
    Coroutine playing;

    const float BarW = 460f;

    static readonly Color Gain = new Color(0.55f, 0.82f, 0.38f);
    static readonly Color Amber = new Color(0.95f, 0.75f, 0.35f);

    void Start() => Build();

    void OnEnable()
    {
        GameEvents.DuelResolved += OnResolved;
        GameEvents.JobResult += OnJob;
        GameEvents.RivalFled += OnFled;
    }

    void OnDisable()
    {
        GameEvents.DuelResolved -= OnResolved;
        GameEvents.JobResult -= OnJob;
        GameEvents.RivalFled -= OnFled;
    }

    void Build()
    {
        canvas = UIFactory.CreateOverlayCanvas("DuelToastCanvas", 130, transform);

        headline = UIFactory.Label(canvas.transform, "", 80, Gain, new Vector2(0, 180));
        stats = UIFactory.Label(canvas.transform, "", 40, UIFactory.Parchment, new Vector2(0, 95));
        Bound(stats, 1200f);
        line = UIFactory.Label(canvas.transform, "", 42, UIFactory.Parchment, new Vector2(0, -250));
        line.fontStyle = FontStyles.Italic;

        barTrack = MakeImage(canvas.transform, new Color(0f, 0f, 0f, 0.55f), BarW, 22f, new Vector2(0, 40));
        barFill = MakeImage(barTrack.transform, Gain, BarW, 22f, Vector2.zero);
        barFill.rectTransform.pivot = new Vector2(0f, 0.5f);
        barFill.rectTransform.anchorMin = barFill.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        barFill.rectTransform.anchoredPosition = new Vector2(-BarW * 0.5f, 0f);

        canvas.gameObject.SetActive(false);
    }

    static void Bound(TMP_Text t, float width)
    {
        t.textWrappingMode = TextWrappingModes.Normal;
        t.rectTransform.sizeDelta = new Vector2(width, t.rectTransform.sizeDelta.y);
    }

    Image MakeImage(Transform parent, Color c, float w, float h, Vector2 pos)
    {
        var go = new GameObject("bar");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = pos;
        return img;
    }

    void OnResolved(bool won, int stepDelta)
    {
        var r = RivalRoster.Current;
        string quote = r == null ? "" : won ? r.DeathLine : r.Gloat;
        line.text = string.IsNullOrEmpty(quote) ? "" : $"{r.Name}: \"{quote}\"";

        headline.text = (stepDelta >= 0 ? $"+{stepDelta}" : stepDelta.ToString()) + " STEPS";
        headline.color = stepDelta >= 0 ? Gain : UIFactory.Rust;

        float reaction = GameFlow.Instance != null ? GameFlow.Instance.LastReaction : -1f;
        float margin = won && DuelController.Instance != null ? DuelController.Instance.LastMargin : -1f;
        float frac = DuelController.Instance != null ? DuelController.Instance.LastMarginFraction : 0f;

        var parts = new System.Collections.Generic.List<string>();
        if (won && r != null) parts.Add($"+${r.Bounty}");
        if (reaction >= 0f) parts.Add($"{reaction:0.000}s");
        if (margin >= 0f) parts.Add($"won by {margin:0.000}s");
        stats.text = string.Join("   ", parts);

        bool showBar = margin >= 0f;
        barTrack.gameObject.SetActive(showBar);
        if (showBar)
        {
            barFill.rectTransform.sizeDelta = new Vector2(Mathf.Max(6f, BarW * frac), 22f);
        }

        Replay();
    }

    void OnJob(int steps, int money)
    {
        headline.text = $"+{steps} STEPS";
        headline.color = Gain;
        stats.text = money > 0 ? $"+${money}" : "";
        line.text = "SIDE JOB DONE";
        barTrack.gameObject.SetActive(false);
        Replay();
    }

    void OnFled(string name)
    {
        headline.text = "THEY FLED!";
        headline.color = Amber;
        stats.text = "";
        line.text = $"{name} lost their nerve and bolted";
        barTrack.gameObject.SetActive(false);
        Replay();
    }

    void Replay()
    {
        if (playing != null)
        {
            StopCoroutine(playing);
        }
        playing = StartCoroutine(Play());
    }

    IEnumerator Play()
    {
        canvas.gameObject.SetActive(true);
        var start = new Vector2(0f, 180f);
        float dur = 1.8f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            float a = 1f - Mathf.Clamp01((k - 0.5f) / 0.5f);
            headline.rectTransform.anchoredPosition = start + new Vector2(0f, 60f * k);
            SetAlpha(headline, a);
            SetAlpha(stats, a);
            SetAlpha(line, a);
            SetImgAlpha(barTrack, a * 0.55f);
            SetImgAlpha(barFill, a);
            yield return null;
        }
        canvas.gameObject.SetActive(false);
        playing = null;
    }

    static void SetAlpha(TMP_Text t, float a)
    {
        var c = t.color;
        c.a = a;
        t.color = c;
    }

    static void SetImgAlpha(Image img, float a)
    {
        if (!img.gameObject.activeSelf)
        {
            return;
        }
        var c = img.color;
        c.a = a;
        img.color = c;
    }
}

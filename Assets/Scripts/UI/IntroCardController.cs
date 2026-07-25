using System.Collections;
using TMPro;
using UnityEngine;

public class IntroCardController : MonoBehaviour
{
    RectTransform root;
    TMP_Text title, sub, taunt;
    Typewriter tauntTyper;
    string tauntText;
    Coroutine playing;

    const float RefW = 1920f;

    void Start() => Build();

    void OnEnable() => GameEvents.RivalIntro += OnIntro;
    void OnDisable() => GameEvents.RivalIntro -= OnIntro;

    void Build()
    {
        var canvas = UIFactory.CreateOverlayCanvas("IntroCanvas", 120, transform);
        root = new GameObject("Card").AddComponent<RectTransform>();
        root.SetParent(canvas.transform, false);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(RefW, 320f);

        var band = root.gameObject.AddComponent<UnityEngine.UI.Image>();
        band.color = new Color(0.08f, 0.05f, 0.04f, 0.9f);

        title = UIFactory.Label(root, "RIVAL APPROACHES", 90, UIFactory.Parchment, new Vector2(0, 70));
        sub = UIFactory.Label(root, "", 40, UIFactory.Rust, new Vector2(0, -10));
        taunt = UIFactory.Label(root, "", 44, UIFactory.Parchment, new Vector2(0, -85));
        taunt.fontStyle = FontStyles.Italic;
        tauntTyper = taunt.gameObject.AddComponent<Typewriter>();
        root.gameObject.SetActive(false);
    }

    void OnIntro(int cur, int total, string name)
    {
        var r = RivalRoster.Current;

        // 1. Title Name
        string displayName = !string.IsNullOrEmpty(name) ? name : (r != null ? r.Name : "RIVAL APPROACHES");
        title.text = displayName.ToUpper();

        // 2. Subtitle Counter
        sub.text = total > 0 ? $"RIVAL {cur} / {total}" : "";

        // 3. Taunt Line (Checks active Goon's line first, falls back to RivalRoster)
        string line = "";
        if (GameFlow.Instance != null && GameFlow.Instance.activeGoon != null)
        {
            line = GameFlow.Instance.activeGoon.DisplayTaunt;
        }
        else if (r != null)
        {
            line = Cowardice.Fleeing ? r.CowardLine : r.Taunt;
        }

        tauntText = string.IsNullOrEmpty(line) ? "" : $"\"{line}\"";
        taunt.text = "";

        if (playing != null)
        {
            StopCoroutine(playing);
        }
        playing = StartCoroutine(Play());
    }

    IEnumerator Play()
    {
        root.gameObject.SetActive(true);
        AudioManager.Instance?.PlaySfx("rival");
        yield return Slide(-RefW, 0f, 0.35f);
        if (!string.IsNullOrEmpty(tauntText))
        {
            tauntTyper.Play(tauntText);
        }
        yield return new WaitForSecondsRealtime(1.6f);
        yield return Slide(0f, RefW, 0.35f);
        root.gameObject.SetActive(false);
        playing = null;
    }

    IEnumerator Slide(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            root.anchoredPosition = new Vector2(Mathf.SmoothStep(from, to, t / dur), 0f);
            yield return null;
        }
        root.anchoredPosition = new Vector2(to, 0f);
    }
}

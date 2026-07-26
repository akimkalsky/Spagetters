using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WantedBoard : MonoBehaviour
{
    Canvas canvas;
    TMP_Text title;
    readonly List<TMP_Text> rows = new();

    static readonly Color Down = new Color(0.5f, 0.45f, 0.4f);

    void Start() => Build();

    void OnEnable()
    {
        GameEvents.WantedChanged += Refresh;
        if (GameFlow.Instance != null)
        {
            GameFlow.Instance.StateChanged += OnState;
        }
    }

    void OnDisable()
    {
        GameEvents.WantedChanged -= Refresh;
        if (GameFlow.Instance != null)
        {
            GameFlow.Instance.StateChanged -= OnState;
        }
    }

    void OnState(GameState s)
    {
        if (canvas == null)
        {
            return;
        }
        bool show = !GameSettings.StoryMode && (s == GameState.Explore || s == GameState.Duel);
        canvas.gameObject.SetActive(show);
        if (show)
        {
            Refresh();
        }
    }

    void Build()
    {
        canvas = UIFactory.CreateOverlayCanvas("WantedCanvas", 52, transform);

        title = UIFactory.Label(canvas.transform, "WANTED", 44, UIFactory.Rust, Vector2.zero);
        Corner(title, new Vector2(40, -40));

        var all = RivalRoster.All;
        for (int i = 0; i < all.Count; i++)
        {
            var row = UIFactory.Label(canvas.transform, "", 30, UIFactory.Parchment, Vector2.zero);
            Corner(row, new Vector2(40, -110 - i * 44));
            rows.Add(row);
        }

        Refresh();
        if (GameFlow.Instance != null)
        {
            OnState(GameFlow.Instance.State);
        }
    }

    static void Corner(TMP_Text t, Vector2 pos)
    {
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(560f, rt.sizeDelta.y);
        t.alignment = TextAlignmentOptions.Left;
    }

    void Refresh()
    {
        var all = RivalRoster.All;
        for (int i = 0; i < rows.Count && i < all.Count; i++)
        {
            var r = all[i];
            if (r.Defeated)
            {
                rows[i].text = $"<s>{r.Name}   ${r.Bounty}</s>   DOWN";
                rows[i].color = Down;
            }
            else
            {
                rows[i].text = $"{r.Name}   ${r.Bounty}";
                rows[i].color = UIFactory.Parchment;
            }
        }
    }
}

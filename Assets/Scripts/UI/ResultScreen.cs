using TMPro;
using UnityEngine;

public class ResultScreen : MonoBehaviour
{
    Canvas canvas;
    TMP_Text headline, sub, quote;
    Typewriter quoteTyper;

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

    void OnState(GameState s)
    {
        bool show = s == GameState.Result;
        canvas.gameObject.SetActive(show);
        if (!show)
        {
            return;
        }

        var flow = GameFlow.Instance;
        var r = RivalRoster.Current;

        if (flow.LastWasCoward)
        {
            headline.text = "THEY FLED";
            headline.color = UIFactory.Parchment;
            sub.text = r != null ? $"{r.Name} lost their nerve" : "lost their nerve";
            string cl = r != null ? r.CowardLine : "";
            quoteTyper.Play(string.IsNullOrEmpty(cl) ? "" : $"{r.Name}: “{cl}”");
            return;
        }

        headline.text = flow.PlayerWon ? "YOU LIVE" : "YOU DIED";
        headline.color = flow.PlayerWon ? UIFactory.Parchment : UIFactory.Rust;

        string flavour = flow.PlayerWon ? "the town is yours" : "buried at noon";
        sub.text = flow.LastReaction >= 0f ? $"{flavour}   {flow.LastReaction:0.000}s" : flavour;

        string line = r == null ? "" : flow.PlayerWon ? r.DeathLine : r.Gloat;
        quoteTyper.Play(string.IsNullOrEmpty(line) ? "" : $"{r.Name}: “{line}”");
    }

    void Build()
    {
        canvas = UIFactory.CreateOverlayCanvas("ResultCanvas", 150, transform);
        UIFactory.FullScreenPanel(canvas.transform, new Color(0.08f, 0.05f, 0.04f, 0.92f));
        headline = UIFactory.Label(canvas.transform, "YOU LIVE", 150, UIFactory.Parchment, new Vector2(0, 250));
        sub = UIFactory.Label(canvas.transform, "", 48, UIFactory.Rust, new Vector2(0, 140));
        quote = UIFactory.Label(canvas.transform, "", 38, UIFactory.Parchment, new Vector2(0, 60));
        quote.fontStyle = FontStyles.Italic;
        quoteTyper = quote.gameObject.AddComponent<Typewriter>();
        UIFactory.MenuButton(canvas.transform, "AGAIN", new Vector2(0, -40), () => GameFlow.Instance.RestartDuel());
        UIFactory.MenuButton(canvas.transform, "MAIN MENU", new Vector2(0, -140), () => GameFlow.Instance.ToMainMenu());
    }
}

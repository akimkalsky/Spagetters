using System.Collections;
using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    Canvas canvas;
    TMP_Text countLabel, drawLabel, roundLabel, feintLabel, steadyLabel, promptLabel, nameLabel, paceLabel, paceCaption, drawHint, streakLabel;
    float drawFlash;
    Coroutine feintRoutine;
    int shownCount = int.MinValue;

    static readonly Color Amber = new Color(0.95f, 0.75f, 0.35f);

    void Start()
    {
        Build();
        GameFlow.Instance.StateChanged += OnState;
        OnState(GameFlow.Instance.State);
    }

    void OnEnable()
    {
        GameEvents.StepsChanged += OnSteps;
        GameEvents.OutOfSteps += OnOutOfSteps;
        GameEvents.DrawWindowOpened += OnDrawWindow;
        GameEvents.RoundChanged += OnRound;
        GameEvents.DuelEnded += OnDuelEnded;
        GameEvents.FeintFlashed += OnFeint;
        GameEvents.SteadyShown += OnSteady;
        GameEvents.Prompt += OnPrompt;
        GameEvents.Pace += OnPace;
        GameEvents.StreakChanged += OnStreak;
    }

    void OnDisable()
    {
        GameEvents.StepsChanged -= OnSteps;
        GameEvents.OutOfSteps -= OnOutOfSteps;
        GameEvents.DrawWindowOpened -= OnDrawWindow;
        GameEvents.RoundChanged -= OnRound;
        GameEvents.DuelEnded -= OnDuelEnded;
        GameEvents.FeintFlashed -= OnFeint;
        GameEvents.SteadyShown -= OnSteady;
        GameEvents.Prompt -= OnPrompt;
        GameEvents.Pace -= OnPace;
        GameEvents.StreakChanged -= OnStreak;
        if (GameFlow.Instance != null)
        {
            GameFlow.Instance.StateChanged -= OnState;
        }
    }

    void OnState(GameState s)
    {
        bool show = s == GameState.Duel || s == GameState.Explore;
        canvas.gameObject.SetActive(show);
        drawLabel.gameObject.SetActive(false);
        drawHint.gameObject.SetActive(false);
        steadyLabel.gameObject.SetActive(false);
        feintLabel.gameObject.SetActive(false);
        ShowPace(false);
        if (s != GameState.Explore)
        {
            promptLabel.gameObject.SetActive(false);
        }

        // --- CHANGE 1: Display Name Fix ---
        Goon active = GameFlow.Instance != null ? GameFlow.Instance.activeGoon : null;
        var r = RivalRoster.Current;

        bool duel = s == GameState.Duel;
        string displayName = "";

        if (active != null)
        {
            displayName = active.DisplayName;
        }
        else if (r != null)
        {
            displayName = r.Name;
        }

        nameLabel.text = duel ? displayName : "";
        nameLabel.gameObject.SetActive(duel && !GameSettings.StoryMode && !string.IsNullOrEmpty(displayName));
    }

    void Build()
    {
        canvas = UIFactory.CreateOverlayCanvas("HUDCanvas", 50, transform);
        countLabel = UIFactory.Label(canvas.transform, "10", 160, UIFactory.Parchment, new Vector2(0, 460));
        roundLabel = UIFactory.Label(canvas.transform, "", 46, UIFactory.Rust, new Vector2(0, 480));
        nameLabel = UIFactory.Label(canvas.transform, "", 56, UIFactory.Parchment, new Vector2(0, 300));

        paceCaption = UIFactory.Label(canvas.transform, "PACES", 46, Amber, new Vector2(0, 200));
        paceCaption.gameObject.SetActive(false);
        paceLabel = UIFactory.Label(canvas.transform, "", 180, Amber, new Vector2(0, 40));
        paceLabel.gameObject.SetActive(false);

        drawLabel = UIFactory.Label(canvas.transform, "DRAW!", 240, UIFactory.Rust, new Vector2(0, 20));
        drawLabel.gameObject.SetActive(false);
        drawHint = UIFactory.Label(canvas.transform, "CLICK  or  E !", 64, Amber, new Vector2(0, -180));
        drawHint.gameObject.SetActive(false);

        feintLabel = UIFactory.Label(canvas.transform, "…", 130, new Color(0.55f, 0.5f, 0.45f, 1f), new Vector2(0, 20));
        feintLabel.gameObject.SetActive(false);
        steadyLabel = UIFactory.Label(canvas.transform, "STEADY…", 110, Amber, new Vector2(0, 20));
        steadyLabel.gameObject.SetActive(false);
        promptLabel = UIFactory.Label(canvas.transform, "", 54, Amber, new Vector2(0, -330));
        promptLabel.gameObject.SetActive(false);

        streakLabel = UIFactory.Label(canvas.transform, "", 44, Amber, Vector2.zero);
        var srt = streakLabel.rectTransform;
        srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(1f, 1f);
        srt.anchoredPosition = new Vector2(-40, -40);
        srt.sizeDelta = new Vector2(360, 60);
        streakLabel.alignment = TextAlignmentOptions.Right;
        streakLabel.gameObject.SetActive(false);
    }

    void OnStreak(int streak)
    {
        bool show = streak >= 2;
        streakLabel.gameObject.SetActive(show);
        if (show)
        {
            streakLabel.text = $"STREAK  x{streak}";
            StartCoroutine(Punch(streakLabel.rectTransform));
        }
    }

    void OnSteps(int steps) => SetCount(steps);
    void OnOutOfSteps() => SetCount(0);

    void SetCount(int value)
    {
        if (value == shownCount)
        {
            return;
        }
        shownCount = value;
        countLabel.text = value.ToString();
    }

    // --- CHANGE 2: Round Label Fix ---
    void OnRound(int cur, int total)
    {
        if (GameSettings.StoryMode || total <= 0)
        {
            roundLabel.text = "";
        }
        else
        {
            roundLabel.text = $"RIVAL {cur} / {total}";
        }
    }

    void OnPace(int n)
    {
        if (n < 0)
        {
            ShowPace(false);
            return;
        }
        paceLabel.text = n.ToString();
        ShowPace(true);
        StartCoroutine(Punch(paceLabel.rectTransform));
    }

    void ShowPace(bool on)
    {
        paceLabel.gameObject.SetActive(on);
        paceCaption.gameObject.SetActive(on);
    }

    void OnDrawWindow()
    {
        ShowPace(false);
        steadyLabel.gameObject.SetActive(false);
        feintLabel.gameObject.SetActive(false);
        drawLabel.gameObject.SetActive(true);
        drawHint.gameObject.SetActive(true);
        drawFlash = 0f;
        StartCoroutine(Punch(drawLabel.rectTransform));
    }

    void OnSteady()
    {
        ShowPace(false);
        promptLabel.gameObject.SetActive(false);
        steadyLabel.gameObject.SetActive(true);
    }

    void OnPrompt(string text)
    {
        bool show = !string.IsNullOrEmpty(text);
        promptLabel.text = show ? text : "";
        promptLabel.gameObject.SetActive(show);
    }

    void OnDuelEnded(bool won, float reaction)
    {
        drawLabel.gameObject.SetActive(false);
        drawHint.gameObject.SetActive(false);
        steadyLabel.gameObject.SetActive(false);
        feintLabel.gameObject.SetActive(false);
        ShowPace(false);
    }

    void OnFeint()
    {
        if (feintRoutine != null)
        {
            StopCoroutine(feintRoutine);
        }
        feintRoutine = StartCoroutine(FlashFeint());
    }

    IEnumerator FlashFeint()
    {
        feintLabel.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(0.14f);
        feintLabel.gameObject.SetActive(false);
    }

    IEnumerator Punch(RectTransform rt)
    {
        float dur = 0.18f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - t / dur;
            rt.localScale = Vector3.one * (1f + 0.4f * k);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    void Update()
    {
        if (canvas.gameObject.activeSelf && GameManager.Instance != null)
        {
            SetCount(GameManager.Instance.steps);
        }

        if (drawLabel.gameObject.activeSelf)
        {
            if (Accessibility.ReduceFlashing)
            {
                drawLabel.color = Color.white;
            }
            else
            {
                drawFlash += Time.deltaTime * 8f;
                drawLabel.color = Color.Lerp(UIFactory.Rust, Color.white, Mathf.PingPong(drawFlash, 1f));
            }
        }
    }
}
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunClock : MonoBehaviour
{
    public static RunClock Instance { get; private set; }

    public const float TotalDaylight = 180f;
    const float TrackW = 760f;

    float daylight;
    bool running, runOver;
    int shownSecond = -1;
    float cawTimer;
    bool riserPlayed;

    public bool Running => running;
    public string Clock => $"{(int)(daylight / 60f)}:{(int)(daylight % 60f):00}";
    public float NightProgress => Mathf.Clamp01(1f - daylight / TotalDaylight);

    Canvas hudCanvas, overCanvas;
    Image fill;
    RectTransform sun;
    TMP_Text timeText, overTitle, overSub, overStats;
    Image[] vultures;
    static Sprite disc, bird;

    void Awake() => Instance = this;

    void Start()
    {
        BuildHud();
        BuildOver();
        ShowHud(false);
    }

    void OnEnable()
    {
        GameEvents.DuelStarted += OnDuelStarted;
        GameEvents.DuelEnded += OnDuelEnded;
        GameEvents.OutOfSteps += OnOutOfSteps;
    }

    void OnDisable()
    {
        GameEvents.DuelStarted -= OnDuelStarted;
        GameEvents.DuelEnded -= OnDuelEnded;
        GameEvents.OutOfSteps -= OnOutOfSteps;
    }

    void OnOutOfSteps()
    {
        if (running && !runOver)
        {
            EndRun(false, "OUT OF STEPS", "you ran the boot leather off your soles");
        }
    }

    public void GameOver(string title, string sub)
    {
        if (!runOver)
        {
            EndRun(false, title, sub);
        }
    }

    void OnDuelStarted()
    {
        if (!running && !runOver)
        {
            StartRun();
        }
    }

    public void StartRun()
    {
        daylight = TotalDaylight;
        runOver = false;
        cawTimer = 0f;
        riserPlayed = false;
        Wallet.Reset();
        Loadout.Reset();
        RivalRoster.ResetDefeated();
        overCanvas.gameObject.SetActive(false);

        running = !GameSettings.StoryMode;
        ShowHud(running);
    }

    public void AddDaylight(float seconds) => daylight += seconds;

    void OnDuelEnded(bool won, float reaction)
    {
        if (!running || !won)
        {
            return;
        }
        RivalRoster.MarkDefeated(RivalRoster.Current);
        if (RivalRoster.AllDefeated())
        {
            EndRun(true);
        }
    }

    void Update()
    {
        if (!running)
        {
            return;
        }
        if (GameFlow.Instance != null && GameFlow.Instance.State == GameState.Paused)
        {
            return;
        }
        daylight -= Time.unscaledDeltaTime;
        if (daylight <= 0f)
        {
            daylight = 0f;
            EndRun(false);
            return;
        }
        UpdateHud();
    }

    void EndRun(bool won, string title = null, string sub = null)
    {
        running = false;
        runOver = true;
        ShowHud(false);
        Time.timeScale = 0f;
        overCanvas.gameObject.SetActive(true);

        int daylightLeft = won ? (int)daylight : 0;
        bool record = Highscore.Report(won, RivalRoster.DefeatedCount, Wallet.TotalEarned, daylightLeft);

        overTitle.text = title ?? (won ? "FASTEST GUN IN THE WEST" : "SUNDOWN");
        overTitle.color = won ? UIFactory.Parchment : UIFactory.Rust;
        overSub.text = record
            ? "a new personal best!"
            : (sub ?? (won ? "you cleared the wall before dark" : "the sun set on your ambitions"));
        overStats.text = Summary(won);
        AudioManager.Instance?.PlaySfx(won ? "win" : "lose");
        AudioManager.Instance?.PlayMusic(won ? "victory_theme" : "gameover_theme", 0.5f);
    }

    string Summary(bool won)
    {
        var lines = new System.Text.StringBuilder();
        lines.AppendLine($"RIVALS DOWN      {RivalRoster.DefeatedCount} / {RivalRoster.Count}");
        lines.AppendLine($"BOUNTY EARNED    ${Wallet.TotalEarned}");
        lines.AppendLine(won ? $"DAYLIGHT LEFT    {Clock}" : "caught by the dark");

        string best = $"BEST   {Highscore.BestRivals} rivals,  ${Highscore.BestBounty}";
        if (Highscore.BestDaylightLeft > 0)
        {
            best += $",  {FmtClock(Highscore.BestDaylightLeft)} left";
        }
        lines.Append(best);
        return lines.ToString();
    }

    static string FmtClock(int sec) => $"{sec / 60}:{sec % 60:00}";

    void ShowHud(bool on) => hudCanvas.gameObject.SetActive(on);

    void UpdateHud()
    {
        float frac = daylight / TotalDaylight;
        fill.rectTransform.sizeDelta = new Vector2(TrackW * frac, 26f);
        fill.color = Color.Lerp(new Color(0.7f, 0.15f, 0.05f), new Color(0.95f, 0.62f, 0.2f), frac);
        sun.anchoredPosition = new Vector2(-TrackW * 0.5f + TrackW * frac, 0f);

        int sec = (int)daylight;
        if (sec != shownSecond)
        {
            shownSecond = sec;
            timeText.text = $"SUNDOWN  {sec / 60}:{sec % 60:00}";
        }

        float night = NightProgress;
        if (night > 0.4f)
        {
            cawTimer -= Time.unscaledDeltaTime;
            if (cawTimer <= 0f)
            {
                cawTimer = Random.Range(7f, 12f);
                AudioManager.Instance?.PlaySfx("caw", 0.5f);
            }
        }
        if (!riserPlayed && night > 0.7f)
        {
            riserPlayed = true;
            AudioManager.Instance?.PlaySfx("riser", 0.6f);
        }

        Vultures(night);
    }

    void Vultures(float night)
    {
        bool show = night > 0.4f;
        for (int i = 0; i < vultures.Length; i++)
        {
            if (vultures[i].gameObject.activeSelf != show)
            {
                vultures[i].gameObject.SetActive(show);
            }
            if (!show)
            {
                continue;
            }
            float ang = Time.unscaledTime * 0.6f + i * Mathf.PI;
            float cy = Mathf.Lerp(360f, 200f, night);
            var pos = new Vector2(Mathf.Cos(ang) * 260f, cy + Mathf.Sin(ang) * 90f);
            var rt = vultures[i].rectTransform;
            rt.anchoredPosition = pos;
            rt.localScale = new Vector3(Mathf.Cos(ang) >= 0f ? 1f : -1f, 1f, 1f) * Mathf.Lerp(0.7f, 1.3f, night);
            var c = vultures[i].color; c.a = Mathf.Clamp01((night - 0.4f) / 0.6f) * 0.7f; vultures[i].color = c;
        }
    }

    void BuildHud()
    {
        hudCanvas = UIFactory.CreateOverlayCanvas("SundownHud", 55, transform);
        timeText = UIFactory.Label(hudCanvas.transform, "SUNDOWN  3:00", 34, UIFactory.Parchment, new Vector2(0, 512));

        var track = new GameObject("Track").AddComponent<Image>();
        track.transform.SetParent(hudCanvas.transform, false);
        track.color = new Color(0f, 0f, 0f, 0.5f);
        track.rectTransform.sizeDelta = new Vector2(TrackW, 26f);
        track.rectTransform.anchoredPosition = new Vector2(0, 470);

        fill = new GameObject("Fill").AddComponent<Image>();
        fill.transform.SetParent(track.transform, false);
        fill.color = new Color(0.95f, 0.62f, 0.2f);
        fill.rectTransform.pivot = new Vector2(0f, 0.5f);
        fill.rectTransform.anchorMin = fill.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        fill.rectTransform.anchoredPosition = new Vector2(-TrackW * 0.5f, 0f);
        fill.rectTransform.sizeDelta = new Vector2(TrackW, 26f);

        sun = new GameObject("Sun").AddComponent<Image>().rectTransform;
        sun.GetComponent<Image>().sprite = Disc();
        sun.GetComponent<Image>().color = new Color(1f, 0.9f, 0.6f);
        sun.SetParent(track.transform, false);
        sun.sizeDelta = new Vector2(40, 40);

        vultures = new Image[2];
        for (int i = 0; i < vultures.Length; i++)
        {
            var v = new GameObject("Vulture").AddComponent<Image>();
            v.transform.SetParent(hudCanvas.transform, false);
            v.sprite = Bird();
            v.raycastTarget = false;
            v.color = new Color(0.1f, 0.08f, 0.08f, 0f);
            v.rectTransform.sizeDelta = new Vector2(90, 56);
            v.gameObject.SetActive(false);
            vultures[i] = v;
        }
    }

    void BuildOver()
    {
        overCanvas = UIFactory.CreateOverlayCanvas("RunOverCanvas", 300, transform);
        UIFactory.FullScreenPanel(overCanvas.transform, new Color(0.07f, 0.04f, 0.03f, 1f));
        overTitle = UIFactory.Label(overCanvas.transform, "SUNDOWN", 84, UIFactory.Parchment, new Vector2(0, 300));
        overSub = UIFactory.Label(overCanvas.transform, "", 44, UIFactory.Rust, new Vector2(0, 200));
        overStats = UIFactory.Label(overCanvas.transform, "", 42, UIFactory.Parchment, new Vector2(0, 40));
        overStats.lineSpacing = 20f;
        UIFactory.MenuButton(overCanvas.transform, "NEW RUN", new Vector2(0, -190), NewRun);
        UIFactory.MenuButton(overCanvas.transform, "MAIN MENU", new Vector2(0, -300), ToMenu);
        overCanvas.gameObject.SetActive(false);
    }

    void NewRun()
    {
        Time.timeScale = 1f;
        RivalRoster.ResetAll();
        StartRun();
        GameFlow.Instance.ToMainMenu();
    }

    void ToMenu()
    {
        Time.timeScale = 1f;
        running = false;
        runOver = false;
        overCanvas.gameObject.SetActive(false);
        GameFlow.Instance.ToMainMenu();
    }

    static Sprite Bird()
    {
        if (bird != null)
        {
            return bird;
        }
        int w = 64, h = 40;
        bird = ProceduralTex.Generate(w, h, (x, y) =>
        {
            float t = x < w / 2 ? (float)x / (w / 2) : (float)(w - x) / (w / 2);
            float wy = h * 0.3f + t * h * 0.4f;
            return Mathf.Abs(y - wy) < 3f ? Color.white : Color.clear;
        });
        return bird;
    }

    static Sprite Disc()
    {
        if (disc != null)
        {
            return disc;
        }
        disc = ProceduralTex.Disc(48, Color.white);
        return disc;
    }
}

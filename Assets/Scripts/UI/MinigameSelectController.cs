using TMPro;
using UnityEngine;

public class MinigameSelectController : MonoBehaviour
{
    public static MinigameSelectController Instance { get; private set; }

    static readonly (string name, string scene, bool ready)[] Games =
    {
        ("DYNAMITE DASH", "DynamiteDash", true),
        ("TARGET RANGE", "TargetRange", true),
        ("BANK SAFE CRACK", "BankCrack", true),
        ("STAGECOACH ESCAPE", "Stagecoach", true),
    };

    Canvas canvas;
    TMP_Text daylight;

    void Awake() => Instance = this;

    void Start()
    {
        Build();
        GameFlow.Instance.StateChanged += OnState;
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
        if (s != GameState.MainMenu)
        {
            canvas.gameObject.SetActive(false);
        }
    }

    public void Open() => canvas.gameObject.SetActive(true);
    public void Close() => canvas.gameObject.SetActive(false);

    void Update()
    {
        if (!canvas.gameObject.activeSelf)
        {
            return;
        }
        var rc = RunClock.Instance;
        string sun = rc != null && rc.Running ? "SUNDOWN  " + rc.Clock + "     " : "";
        daylight.text = sun + $"${Wallet.Money}";
    }

    void Build()
    {
        canvas = UIFactory.CreateOverlayCanvas("MinigameSelectCanvas", 106, transform);
        UIFactory.FullScreenPanel(canvas.transform, new Color(0.09f, 0.06f, 0.05f, 1f));
        UIFactory.Label(canvas.transform, "SIDE JOBS", 96, UIFactory.Parchment, new Vector2(0, 340));
        daylight = UIFactory.Readout(canvas.transform);

        float y = 150f;
        foreach (var g in Games)
        {
            string label = g.ready ? g.name : g.name + "   (SOON)";
            var scene = g.scene;
            var btn = UIFactory.MenuButton(canvas.transform, label, new Vector2(0, y),
                () =>
                {
                    if (g.ready)
                    {
                        Close();
                        GameFlow.Instance.StartMinigame(scene);
                    }
                }, 760f);
            btn.interactable = g.ready;
            y -= 110f;
        }

        UIFactory.MenuButton(canvas.transform, "BACK", new Vector2(0, y - 30f), Close, 760f);
        canvas.gameObject.SetActive(false);
    }
}

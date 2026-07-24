using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    Canvas canvas;
    TMP_Text countLabel, drawLabel, roundLabel;
    float drawFlash;

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
    }

    void OnDisable()
    {
        GameEvents.StepsChanged -= OnSteps;
        GameEvents.OutOfSteps -= OnOutOfSteps;
        GameEvents.DrawWindowOpened -= OnDrawWindow;
        GameEvents.RoundChanged -= OnRound;
        GameEvents.DuelEnded -= OnDuelEnded;
        if (GameFlow.Instance != null)
        {
            GameFlow.Instance.StateChanged -= OnState;
        }
    }

    void OnState(GameState s)
    {
        bool show = s == GameState.Duel;
        canvas.gameObject.SetActive(show);
        if (show)
        {
            drawLabel.gameObject.SetActive(false);
        }
    }

    void Build()
    {
        canvas = UIFactory.CreateOverlayCanvas("HUDCanvas", 50, transform);
        countLabel = UIFactory.Label(canvas.transform, "10", 160, UIFactory.Parchment, new Vector2(0, 380));
        roundLabel = UIFactory.Label(canvas.transform, "", 46, UIFactory.Rust, new Vector2(0, 480));
        drawLabel = UIFactory.Label(canvas.transform, "DRAW!", 220, UIFactory.Rust, new Vector2(0, 0));
        drawLabel.gameObject.SetActive(false);
    }

    void OnSteps(int steps) => countLabel.text = steps.ToString();
    void OnOutOfSteps() => countLabel.text = "0";
    void OnRound(int cur, int total) => roundLabel.text = $"RIVAL {cur} / {total}";

    void OnDrawWindow()
    {
        drawLabel.gameObject.SetActive(true);
        drawFlash = 0f;
    }

    void OnDuelEnded(bool won, float reaction) => drawLabel.gameObject.SetActive(false);

    void Update()
    {
        if (canvas.gameObject.activeSelf && GameManager.Instance != null)
        {
            countLabel.text = GameManager.Instance.steps.ToString();
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

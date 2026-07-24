using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    Canvas canvas;
    GameObject howTo;
    RectTransform emblemRt;
    static Sprite emblem;

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

    void OnState(GameState s) => canvas.gameObject.SetActive(s == GameState.MainMenu);

    void Update()
    {
        if (emblemRt == null || !canvas.gameObject.activeSelf)
        {
            return;
        }
        emblemRt.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 1.2f) * 0.02f);
    }

    void Build()
    {
        canvas = UIFactory.CreateOverlayCanvas("MainMenuCanvas", 100, transform);

        var mark = new GameObject("Emblem").AddComponent<Image>();
        mark.transform.SetParent(canvas.transform, false);
        mark.sprite = Emblem();
        mark.color = new Color(0.95f, 0.6f, 0.2f, 0.9f);
        mark.rectTransform.sizeDelta = new Vector2(520, 520);
        mark.rectTransform.anchoredPosition = new Vector2(0, 320);
        emblemRt = mark.rectTransform;

        UIFactory.Label(canvas.transform, "SPAGETTERS", 132, UIFactory.Parchment, new Vector2(0, 320));

        UIFactory.MenuButton(canvas.transform, "VENTURE OUT", new Vector2(0, 130),
                             () => RivalSelectController.Instance.Open());
        UIFactory.MenuButton(canvas.transform, "SIDE JOBS", new Vector2(0, 30),
                             () => MinigameSelectController.Instance.Open());
        UIFactory.MenuButton(canvas.transform, "STORE", new Vector2(0, -70),
                             () => StoreController.Instance.Open());
        UIFactory.MenuButton(canvas.transform, "HOW TO PLAY", new Vector2(0, -170),
                             () => howTo.SetActive(true));
        UIFactory.MenuButton(canvas.transform, "SETTINGS", new Vector2(0, -270),
                             () => SettingsController.Instance.Open());
        UIFactory.MenuButton(canvas.transform, "QUIT", new Vector2(0, -370),
                             () => GameFlow.Instance.Quit());

        BuildHowTo();
    }

    static Sprite Emblem()
    {
        if (emblem != null)
        {
            return emblem;
        }
        int size = 256, rays = 12;
        float c = size * 0.5f, R = size * 0.2f, rayLen = size * 0.26f;
        emblem = ProceduralTex.Generate(size, size, (x, y) =>
        {
            float dx = x - c, dy = y - c;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float a = 0f;
            if (dist <= R)
            {
                a = 1f;
            }
            else if (dist <= R + rayLen)
            {
                float t = (dist - R) / rayLen;
                float ang = Mathf.Atan2(dy, dx);
                float spacing = 2f * Mathf.PI / rays;
                float nearest = Mathf.Round(ang / spacing) * spacing;
                float diff = Mathf.Abs(Mathf.DeltaAngle(ang * Mathf.Rad2Deg, nearest * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
                if (diff < 0.14f * (1f - t))
                {
                    a = 1f - t;
                }
            }
            return new Color(1f, 1f, 1f, a);
        });
        return emblem;
    }

    void BuildHowTo()
    {
        var panel = UIFactory.FullScreenPanel(canvas.transform, UIFactory.Dim);
        howTo = panel.gameObject;
        UIFactory.Label(howTo.transform, "HOW TO PLAY", 90, UIFactory.Parchment, new Vector2(0, 280));
        UIFactory.Label(howTo.transform,
            "Walk out with WASD as the count ticks down.\nWhen it hits zero, DRAW is called.\nFire faster than your rival to live.",
            44, UIFactory.Parchment, new Vector2(0, 40)).textWrappingMode = TextWrappingModes.Normal;
        UIFactory.MenuButton(howTo.transform, "BACK", new Vector2(0, -260),
                             () => howTo.SetActive(false));
        howTo.SetActive(false);
    }
}

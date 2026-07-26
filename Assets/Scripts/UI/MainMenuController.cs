using TMPro;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
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

        UIFactory.MenuButton(canvas.transform, "BOUNT BONANZA", new Vector2(0, 30),
    () =>
    {
        GameSettings.StoryMode = false;

        RivalSelectController.Instance.Open();
    });
        UIFactory.MenuButton(canvas.transform, "STORY MODE", new Vector2(0, 130),
    () =>
    {
        GameSettings.StoryMode = true;
        GameFlow.Instance.EnterWorld();
    });
        UIFactory.MenuButton(canvas.transform, "SIDE JOBS", new Vector2(0, -70),
                             () => MinigameSelectController.Instance.Open());
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
        var panel = UIFactory.FullScreenPanel(canvas.transform, new Color(0.06f, 0.04f, 0.03f, 1f));
        howTo = panel.gameObject;

        Box(howTo.transform, new Vector2(1380, 860), Vector2.zero, UIFactory.Rust);
        Box(howTo.transform, new Vector2(1360, 840), Vector2.zero, new Color(0.12f, 0.08f, 0.06f, 1f));

        UIFactory.Label(howTo.transform, "HOW TO PLAY", 92, UIFactory.Rust, new Vector2(0, 330));
        var tag = UIFactory.Label(howTo.transform, "read it 'fore you go and get yourself killed", 32, UIFactory.Parchment, new Vector2(0, 262));
        tag.fontStyle = FontStyles.Italic;
        Box(howTo.transform, new Vector2(720, 3), new Vector2(0, 226), UIFactory.Rust);

        const string key = "#F2BF59";
        const string bad = "#C05A34";
        var body = UIFactory.Label(howTo.transform,
            $"<color={key}>W / S</color>  walk,   <color={key}>A / D</color>  turn.\n\n" +
            $"Get close and hit <color={key}>E</color> (or click) to call a rival out.\n\n" +
            $"Walk your paces as the count winds down.\n\n" +
            $"On <color={key}>DRAW!</color> - out-draw him. Fire early and you're buried.\n\n" +
            $"Odd jobs pay coin and daylight, spent at the <color={key}>store</color> (pause menu).\n\n" +
            $"Put every rival in the dirt <color={bad}>before sundown</color>.",
            32, UIFactory.Parchment, new Vector2(0, -50), TextAlignmentOptions.Left);
        body.rectTransform.sizeDelta = new Vector2(1200, 480);
        body.textWrappingMode = TextWrappingModes.Normal;
        body.fontStyle = FontStyles.Normal;
        body.characterSpacing = 0f;
        body.lineSpacing = 8f;

        UIFactory.MenuButton(howTo.transform, "CONTINUE", new Vector2(0, -370),
                             () => howTo.SetActive(false));

        bool firstTime = PlayerPrefs.GetInt("seen_howto", 0) == 0;
        howTo.SetActive(firstTime);
        if (firstTime)
        {
            PlayerPrefs.SetInt("seen_howto", 1);
            PlayerPrefs.Save();
        }
    }

    static Image Box(Transform parent, Vector2 size, Vector2 pos, Color color)
    {
        var img = new GameObject("Box").AddComponent<Image>();
        img.transform.SetParent(parent, false);
        img.color = color;
        img.raycastTarget = false;
        img.rectTransform.sizeDelta = size;
        img.rectTransform.anchoredPosition = pos;
        return img;
    }
}

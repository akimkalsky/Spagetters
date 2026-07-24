using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RivalSelectController : MonoBehaviour
{
    public static RivalSelectController Instance { get; private set; }

    Canvas canvas;
    RectTransform row;
    TMP_Text daylight;
    readonly List<RectTransform> posters = new();
    static Sprite dust;
    static Sprite starSprite;

    void Awake() => Instance = this;

    void Start()
    {
        Build();
        GameFlow.Instance.StateChanged += OnState;
    }

    void OnEnable() => GameEvents.DuelEnded += OnDuelEnded;

    void OnDisable()
    {
        GameEvents.DuelEnded -= OnDuelEnded;
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

    void OnDuelEnded(bool won, float reaction)
    {
        if (won)
        {
            RivalRoster.MarkDefeated(RivalRoster.Current);
        }
    }

    public void Open()
    {
        StopAllCoroutines();
        RebuildRow();
        canvas.gameObject.SetActive(true);
        StartCoroutine(SlamSequence());
    }

    public void Close() => canvas.gameObject.SetActive(false);

    void Build()
    {
        canvas = UIFactory.CreateOverlayCanvas("RivalSelectCanvas", 105, transform);
        UIFactory.FullScreenPanel(canvas.transform, new Color(0.09f, 0.06f, 0.05f, 1f));
        UIFactory.Label(canvas.transform, "BOUNTIES", 96, UIFactory.Parchment, new Vector2(0, 400));
        UIFactory.Label(canvas.transform, "pick your mark", 34, UIFactory.Rust, new Vector2(0, 330));
        daylight = UIFactory.Readout(canvas.transform);

        row = new GameObject("Row").AddComponent<RectTransform>();
        row.SetParent(canvas.transform, false);
        row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f);

        UIFactory.MenuButton(canvas.transform, "BACK", new Vector2(0, -420), Close);
        canvas.gameObject.SetActive(false);
    }

    void RebuildRow()
    {
        posters.Clear();
        for (int i = row.childCount - 1; i >= 0; i--) Destroy(row.GetChild(i).gameObject);

        var all = RivalRoster.All;
        float spacing = 330f;
        float startX = -(all.Count - 1) * 0.5f * spacing;
        for (int i = 0; i < all.Count; i++)
            Poster(all[i], startX + i * spacing);
    }

    void Poster(Rival r, float x)
    {
        var root = new GameObject("Poster_" + r.Index).AddComponent<RectTransform>();
        root.SetParent(row, false);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(300, 440);
        root.anchoredPosition = new Vector2(x, 0);
        root.localScale = Vector3.zero;
        posters.Add(root);
        var rnd = new System.Random(r.Seed);
        float tilt = (float)(rnd.NextDouble() - 0.5) * 6f;
        root.localRotation = Quaternion.Euler(0, 0, tilt);

        var img = root.gameObject.AddComponent<Image>();
        img.color = UIFactory.Parchment;
        var btn = root.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(() => Choose(r));
        root.gameObject.AddComponent<UISfx>();
        root.gameObject.AddComponent<PosterHover>().restRotation = tilt;

        UIFactory.Label(root, "WANTED", 42, UIFactory.Ink, new Vector2(0, 175));
        UIFactory.Label(root, "DEAD OR ALIVE", 20, UIFactory.Rust, new Vector2(0, 140));

        var port = new GameObject("Portrait").AddComponent<Image>();
        port.transform.SetParent(root, false);
        port.sprite = Bust(256, UIFactory.Ink, r.Seed);
        port.raycastTarget = false;
        port.rectTransform.sizeDelta = new Vector2(190, 190);
        port.rectTransform.anchoredPosition = new Vector2(0, 40);

        UIFactory.Label(root, r.Name.ToUpper(), 26, UIFactory.Ink, new Vector2(0, -92));
        UIFactory.Label(root, $"${r.Bounty:N0}", 32, UIFactory.Rust, new Vector2(0, -130));
        int stars = r.Index + 1;
        for (int s = 0; s < 5; s++)
        {
            var star = new GameObject("Star").AddComponent<Image>();
            star.transform.SetParent(root, false);
            star.sprite = StarSprite();
            star.raycastTarget = false;
            star.color = s < stars ? new Color(0.9f, 0.7f, 0.2f) : new Color(0.32f, 0.24f, 0.16f);
            star.rectTransform.sizeDelta = new Vector2(26, 26);
            star.rectTransform.anchoredPosition = new Vector2((s - 2) * 30f, -168);
        }
        var tag = UIFactory.Label(root, r.Tagline, 18, UIFactory.Rust, new Vector2(0, -202));
        tag.fontStyle = FontStyles.Italic;

        if (r.Defeated)
        {
            var stamp = UIFactory.Label(root, "DEFEATED", 46, new Color(0.62f, 0.12f, 0.10f, 0.92f), Vector2.zero);
            stamp.rectTransform.localRotation = Quaternion.Euler(0, 0, -18f);
        }
    }

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

    void Choose(Rival r)
    {
        RivalRoster.Select(r);
        Close();
        GameFlow.Instance.StartDuel();
    }

    IEnumerator SlamSequence()
    {
        foreach (var p in posters) p.localScale = Vector3.zero;
        foreach (var p in posters)
        {
            StartCoroutine(Slam(p));
            yield return new WaitForSecondsRealtime(0.09f);
        }
    }

    IEnumerator Slam(RectTransform rt)
    {
        StartCoroutine(Puff(new Vector2(rt.anchoredPosition.x, -190f)));
        AudioManager.Instance?.PlaySfx("footstep", 0.6f);
        float dur = 0.22f, t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            rt.localScale = Vector3.one * OutBack(t / dur);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    IEnumerator Puff(Vector2 at)
    {
        const int n = 7;
        var rts = new RectTransform[n];
        var vel = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            var img = new GameObject("Dust").AddComponent<Image>();
            img.transform.SetParent(row, false);
            img.sprite = DustSprite();
            img.raycastTarget = false;
            img.color = new Color(0.82f, 0.72f, 0.55f, 0.8f);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.one * Random.Range(30f, 60f);
            rt.anchoredPosition = at + Random.insideUnitCircle * 20f;
            rts[i] = rt;
            vel[i] = new Vector2(Random.Range(-70f, 70f), Random.Range(20f, 90f));
        }

        float dur = 0.5f, t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = t / dur;
            for (int i = 0; i < n; i++)
            {
                rts[i].anchoredPosition += vel[i] * Time.unscaledDeltaTime;
                rts[i].localScale = Vector3.one * Mathf.Lerp(0.6f, 1.5f, p);
                var img = rts[i].GetComponent<Image>();
                var c = img.color; c.a = 0.8f * (1f - p); img.color = c;
            }
            yield return null;
        }
        for (int i = 0; i < n; i++) if (rts[i] != null)
        {
            Destroy(rts[i].gameObject);
        }
    }

    static float OutBack(float p)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        p -= 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }

    static Sprite DustSprite()
    {
        if (dust != null)
        {
            return dust;
        }
        dust = ProceduralTex.SoftDisc(64, Color.white);
        return dust;
    }

    static Sprite StarSprite()
    {
        if (starSprite == null)
        {
            starSprite = ProceduralTex.Star(64, Color.white);
        }
        return starSprite;
    }

    static Sprite Bust(int size, Color col, int seed)
    {
        var rnd = new System.Random(seed);
        float cx = size * 0.5f;
        float headR = size * (0.16f + (float)rnd.NextDouble() * 0.03f);
        float headCy = size * 0.52f;
        float brimY = headCy + headR * 0.75f;
        float brimW = size * (0.42f + (float)rnd.NextDouble() * 0.06f);
        float brimH = size * 0.045f;
        float crownW = headR * 1.5f;
        float crownTop = brimY + headR * (1f + (float)rnd.NextDouble() * 0.5f);
        float shRx = size * 0.44f, shRy = size * 0.40f, shCy = size * 0.02f;

        return ProceduralTex.Generate(size, size, (x, y) =>
        {
            float dx = x - cx;
            bool solid = false;

            if (dx * dx + (y - headCy) * (y - headCy) <= headR * headR)
            {
                solid = true;
            }
            if (Mathf.Abs(dx) < headR * 0.35f && y < headCy && y > shCy + shRy * 0.7f)
            {
                solid = true;
            }
            if ((dx * dx) / (shRx * shRx) + ((y - shCy) * (y - shCy)) / (shRy * shRy) <= 1f)
            {
                solid = true;
            }

            float bd = (y - brimY) / brimH;
            if (Mathf.Abs(bd) <= 1f && Mathf.Abs(dx) < brimW * Mathf.Sqrt(1f - bd * bd))
            {
                solid = true;
            }
            if (y >= brimY && y <= crownTop && Mathf.Abs(dx) < crownW * 0.5f)
            {
                solid = true;
            }

            return solid ? col : Color.clear;
        });
    }
}

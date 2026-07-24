using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Minimap : MonoBehaviour
{
    public float worldRange = 45f;
    public float radius = 95f;

    static readonly Color Easy = new Color(0.42f, 0.8f, 0.36f);
    static readonly Color Hard = new Color(0.85f, 0.22f, 0.15f);
    static readonly Color Job = new Color(0.45f, 0.62f, 0.95f);

    Canvas canvas;
    RectTransform panel;
    Transform player;
    readonly List<Image> blips = new();
    Goon[] goons = new Goon[0];
    JobStation[] jobs = new JobStation[0];
    float refreshTimer;
    float cos, sin;
    int blipIndex;

    void Start() => Build();

    void Build()
    {
        canvas = UIFactory.CreateOverlayCanvas("MinimapCanvas", 52, transform);
        panel = MakeImage(canvas.transform, new Color(0.05f, 0.04f, 0.03f, 0.55f), radius * 2f, radius * 2f).rectTransform;
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1f, 0f);
        panel.anchoredPosition = new Vector2(-34f, 34f);

        var facing = MakeImage(panel, UIFactory.Parchment, 4f, 16f);
        facing.rectTransform.anchoredPosition = new Vector2(0f, 9f);
        var dot = MakeImage(panel, UIFactory.Parchment, 12f, 12f);
        dot.rectTransform.anchoredPosition = Vector2.zero;
    }

    Image MakeImage(Transform parent, Color c, float w, float h)
    {
        var go = new GameObject("blip");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        return img;
    }

    void Update()
    {
        bool explore = GameFlow.Instance != null && GameFlow.Instance.State == GameState.Explore;
        if (canvas.gameObject.activeSelf != explore)
        {
            canvas.gameObject.SetActive(explore);
        }
        if (!explore)
        {
            return;
        }

        if (player == null)
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                player = pc.transform;
            }
        }
        if (player == null)
        {
            return;
        }

        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = 0.25f;
            goons = FindObjectsByType<Goon>(FindObjectsSortMode.None);
            jobs = FindObjectsByType<JobStation>(FindObjectsSortMode.None);
        }

        float yaw = player.eulerAngles.y * Mathf.Deg2Rad;
        cos = Mathf.Cos(yaw);
        sin = Mathf.Sin(yaw);

        EnsureBlips(goons.Length + jobs.Length);
        blipIndex = 0;
        foreach (var g in goons)
        {
            if (g != null && g.isActiveAndEnabled)
            {
                Place(g.transform.position, DifficultyColor(g));
            }
        }
        foreach (var j in jobs)
        {
            if (j != null && j.isActiveAndEnabled)
            {
                Place(j.transform.position, Job);
            }
        }
        for (int i = blipIndex; i < blips.Count; i++)
        {
            if (blips[i].enabled)
            {
                blips[i].enabled = false;
            }
        }
    }

    void Place(Vector3 worldPos, Color color)
    {
        if (blipIndex >= blips.Count)
        {
            return;
        }
        var blip = blips[blipIndex++];
        blip.enabled = true;
        Vector3 rel = worldPos - player.position;
        Vector2 p = new Vector2(rel.x * cos - rel.z * sin, rel.x * sin + rel.z * cos) / worldRange * radius;
        if (p.sqrMagnitude > radius * radius)
        {
            p = p.normalized * radius;
        }
        blip.rectTransform.anchoredPosition = p;
        blip.color = color;
    }

    void EnsureBlips(int n)
    {
        while (blips.Count < n)
        {
            blips.Add(MakeImage(panel, Color.white, 11f, 11f));
        }
    }

    static Color DifficultyColor(Goon g)
    {
        var r = g.GetRival();
        if (r == null)
        {
            return Color.white;
        }
        float t = Mathf.InverseLerp(0.18f, 0.45f, r.Reaction);
        return Color.Lerp(Hard, Easy, t);
    }
}

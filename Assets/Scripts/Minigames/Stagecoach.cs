using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Stagecoach : MonoBehaviour
{
    enum Phase { Playing, Over }

    static readonly float[] Lanes = { -3f, 0f, 3f };
    const float LineTime = 30f;
    const float PosseStart = 18f;
    const float HitPenalty = 5f;
    const float Recover = 0.6f;
    const float SpawnEvery = 0.9f;

    Phase phase;
    float lineClock;
    float posse;
    int lane = 1;
    float spawnTimer;
    float speed = 22f;
    int lastTick;

    Transform coach, chase;
    Camera cam;
    readonly List<(Transform t, int lane, bool counted)> obstacles = new();
    readonly List<Transform> stripes = new();

    Canvas hud;
    TMP_Text lineLabel, posseLabel;
    GameObject resultPanel;
    TMP_Text resultTitle, resultSub;

    void Awake() => GameFlow.Instance?.EnterMinigame();

    void Start()
    {
        BuildWorld();
        BuildHud();
        Restart();
    }

    void Restart()
    {
        phase = Phase.Playing;
        lineClock = LineTime;
        posse = PosseStart;
        lane = 1;
        speed = 22f;
        spawnTimer = 0f;
        lastTick = Mathf.CeilToInt(LineTime);
        foreach (var o in obstacles) if (o.t != null)
        {
            Destroy(o.t.gameObject);
        }
        obstacles.Clear();
        coach.position = new Vector3(Lanes[lane], 0.6f, 0f);
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
        AudioManager.Instance?.PlayAmbient("wind", 0.2f);
        UpdateHud();
    }

    void BuildWorld()
    {
        cam = Camera.main;
        if (cam == null)
        {
            cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
        }
        cam.transform.position = new Vector3(0, 4.5f, -8f);
        cam.transform.rotation = Quaternion.Euler(12f, 0f, 0f);

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(transform);
        ground.transform.localScale = new Vector3(2.4f, 1f, 8f);
        ground.transform.position = new Vector3(0, 0, 30f);
        Destroy(ground.GetComponent<Collider>());
        Tint(ground, new Color(0.62f, 0.47f, 0.3f), 0.15f);

        for (int i = 0; i < 12; i++)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(s.GetComponent<Collider>());
            s.transform.SetParent(transform);
            s.transform.localScale = new Vector3(0.3f, 0.02f, 2f);
            s.transform.position = new Vector3(0, 0.02f, i * 6f);
            Tint(s, new Color(0.5f, 0.38f, 0.24f), 0.1f);
            stripes.Add(s.transform);
        }

        coach = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        coach.name = "Coach";
        coach.SetParent(transform);
        coach.localScale = new Vector3(1.4f, 1.2f, 2f);
        Destroy(coach.GetComponent<Collider>());
        Tint(coach.gameObject, new Color(0.5f, 0.28f, 0.14f), 0.35f);

        chase = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        chase.name = "Posse";
        chase.SetParent(transform);
        chase.localScale = new Vector3(2.4f, 1.4f, 1.4f);
        Destroy(chase.GetComponent<Collider>());
        Tint(chase.gameObject, new Color(0.15f, 0.12f, 0.12f), 0.2f);

        var light = new GameObject("Sun").AddComponent<Light>();
        light.transform.SetParent(transform);
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(50f, 15f, 0f);
    }

    static void Tint(GameObject go, Color col, float emit)
    {
        var mat = go.GetComponent<Renderer>().material;
        mat.SetColor("_BaseColor", col);
        mat.SetColor("_EmissiveColor", col * emit);
        mat.color = col;
    }

    void BuildHud()
    {
        hud = UIFactory.CreateOverlayCanvas("CoachHud", 60, transform);
        lineLabel = UIFactory.Label(hud.transform, "30", 150, UIFactory.Parchment, new Vector2(0, 400));
        UIFactory.Label(hud.transform, "COUNTY LINE", 34, UIFactory.Rust, new Vector2(0, 300));
        posseLabel = UIFactory.Label(hud.transform, "", 44, UIFactory.Rust, new Vector2(0, -430));
        UIFactory.Label(hud.transform, "A / D  DODGE", 30, UIFactory.Rust, new Vector2(0, -490));

        var panel = UIFactory.FullScreenPanel(hud.transform, new Color(0.08f, 0.05f, 0.04f, 0.92f));
        resultPanel = panel.gameObject;
        resultTitle = UIFactory.Label(resultPanel.transform, "", 150, UIFactory.Parchment, new Vector2(0, 200));
        resultSub = UIFactory.Label(resultPanel.transform, "", 48, UIFactory.Rust, new Vector2(0, 90));
        UIFactory.MenuButton(resultPanel.transform, "AGAIN", new Vector2(0, -40), Restart);
        UIFactory.MenuButton(resultPanel.transform, "MENU", new Vector2(0, -150), Leave);
        resultPanel.SetActive(false);
    }

    void Update()
    {
        if (phase != Phase.Playing)
        {
            return;
        }

        Steer();
        ScrollStripes();
        MoveObstacles();
        Spawn();

        speed += Time.deltaTime * 0.6f;
        posse = Mathf.Min(PosseStart, posse + Recover * Time.deltaTime);
        chase.position = new Vector3(0, 0.7f, -4f - posse * 0.35f);

        lineClock -= Time.deltaTime;
        int whole = Mathf.CeilToInt(Mathf.Max(0f, lineClock));
        lineLabel.text = whole.ToString();
        lineLabel.color = lineClock < 5f ? Color.Lerp(UIFactory.Rust, Color.white, Mathf.PingPong(Time.time * 6f, 1f)) : UIFactory.Parchment;
        if (whole < lastTick)
        {
            lastTick = whole;
            AudioManager.Instance?.PlaySfx("tick");
        }

        UpdateHud();
        if (posse <= 0f)
        {
            End(false);
            return;
        }
        if (lineClock <= 0f)
        {
            End(true);
        }
    }

    void Steer()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)
            {
                lane = Mathf.Max(0, lane - 1);
            }
            if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame)
            {
                lane = Mathf.Min(Lanes.Length - 1, lane + 1);
            }
        }
        var p = coach.position;
        p.x = Mathf.MoveTowards(p.x, Lanes[lane], 16f * Time.deltaTime);
        coach.position = p;
    }

    void ScrollStripes()
    {
        foreach (var s in stripes)
        {
            var p = s.position;
            p.z -= speed * Time.deltaTime;
            if (p.z < -6f)
            {
                p.z += 72f;
            }
            s.position = p;
        }
    }

    void Spawn()
    {
        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
        {
            return;
        }
        spawnTimer = SpawnEvery;

        int obLane = Random.Range(0, Lanes.Length);
        var ob = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(ob.GetComponent<Collider>());
        ob.transform.SetParent(transform);
        ob.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        ob.transform.position = new Vector3(Lanes[obLane], 0.8f, 60f);
        Tint(ob, new Color(0.2f, 0.45f, 0.2f), 0.3f);
        obstacles.Add((ob.transform, obLane, false));
    }

    void MoveObstacles()
    {
        for (int i = obstacles.Count - 1; i >= 0; i--)
        {
            var o = obstacles[i];
            if (o.t == null)
            {
                obstacles.RemoveAt(i);
                continue;
            }
            var p = o.t.position;
            p.z -= speed * Time.deltaTime;
            o.t.position = p;

            if (!o.counted && p.z <= 0.5f)
            {
                obstacles[i] = (o.t, o.lane, true);
                if (o.lane == lane)
                {
                    Hit(o.t);
                }
            }
            if (p.z < -6f)
            {
                Destroy(o.t.gameObject);
                obstacles.RemoveAt(i);
            }
        }
    }

    void Hit(Transform ob)
    {
        posse -= HitPenalty;
        AudioManager.Instance?.PlaySfx("footstep");
        AudioManager.Instance?.PlaySfx("gunshot", 0.4f);
        Destroy(ob.gameObject);
    }

    void UpdateHud()
    {
        int pips = Mathf.Clamp(Mathf.CeilToInt(posse / PosseStart * 6f), 0, 6);
        posseLabel.text = "POSSE " + new string('●', 6 - pips) + new string('○', pips);
    }

    void End(bool escaped)
    {
        phase = Phase.Over;
        resultPanel.SetActive(true);
        int payout = escaped ? 300 + Mathf.RoundToInt(posse * 10f) : 0;
        if (payout > 0)
        {
            Wallet.Add(payout);
        }
        resultTitle.text = escaped ? "ESCAPED" : "CAUGHT";
        resultTitle.color = escaped ? UIFactory.Parchment : UIFactory.Rust;
        resultSub.text = escaped ? $"you crossed the line   +${payout}" : "the posse ran you down";
        AudioManager.Instance?.PlaySfx(escaped ? "win" : "lose");
    }

    void Leave()
    {
        if (GameFlow.Instance != null)
        {
            GameFlow.Instance.ToMainMenu();
        }
        else
        {
            Restart();
        }
    }
}

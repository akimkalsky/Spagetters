using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DynamiteDash : MonoBehaviour
{
    enum Phase { Playing, Over }

    const float MinX = -9f, MaxX = 9f, MinZ = -6f, MaxZ = 6f;
    const float Fuse = 20f;
    const float PlayerSpeed = 8f;

    Phase phase;
    float fuse;
    int loot, lootTotal;
    float shake;
    int lastTick;

    Transform player, exit;
    Camera cam;
    readonly List<Transform> coins = new();

    Canvas hud;
    TMP_Text fuseLabel, lootLabel;
    GameObject resultPanel;
    TMP_Text resultTitle, resultSub;

    void Awake() => GameFlow.Instance?.EnterMinigame();

    void Start()
    {
        BuildArena();
        BuildHud();
        Restart();
    }

    void Restart()
    {
        phase = Phase.Playing;
        fuse = Fuse;
        loot = 0;
        lastTick = Mathf.CeilToInt(Fuse);
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
        Time.timeScale = 1f;

        foreach (var c in coins) c.gameObject.SetActive(true);
        lootTotal = coins.Count;
        player.position = new Vector3(0, 0.7f, MinZ + 1f);
        AudioManager.Instance?.PlayAmbient("wind", 0.15f);
    }

    void BuildArena()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(transform);
        ground.transform.localScale = new Vector3((MaxX - MinX) / 10f + 0.4f, 1f, (MaxZ - MinZ) / 10f + 0.4f);
        Tint(ground, new Color(0.55f, 0.42f, 0.28f));

        exit = MakePad(new Vector3(0, 0.02f, MaxZ + 0.5f), new Color(0.2f, 0.7f, 0.3f), "Exit").transform;

        player = GameObject.CreatePrimitive(PrimitiveType.Capsule).transform;
        player.name = "Player";
        player.SetParent(transform);
        player.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        Tint(player.gameObject, new Color(0.85f, 0.78f, 0.6f));
        Destroy(player.GetComponent<Collider>());

        var rnd = new System.Random(7);
        for (int i = 0; i < 10; i++)
        {
            var pos = new Vector3(
                Mathf.Lerp(MinX + 1, MaxX - 1, (float)rnd.NextDouble()),
                0.12f,
                Mathf.Lerp(MinZ + 1, MaxZ - 1, (float)rnd.NextDouble()));
            var coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = "Loot";
            coin.transform.SetParent(transform);
            coin.transform.position = pos;
            coin.transform.localScale = new Vector3(0.5f, 0.08f, 0.5f);
            Destroy(coin.GetComponent<Collider>());
            Tint(coin, new Color(0.9f, 0.75f, 0.2f));
            coins.Add(coin.transform);
        }

        var light = new GameObject("Sun").AddComponent<Light>();
        light.transform.SetParent(transform);
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
        light.intensity = 1.1f;

        cam = Camera.main;
        if (cam == null)
        {
            cam = new GameObject("Main Camera").AddComponent<Camera>();
        }
        cam.transform.position = new Vector3(0, 17f, -13f);
        cam.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
    }

    GameObject MakePad(Vector3 pos, Color col, string name)
    {
        var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pad.name = name;
        pad.transform.SetParent(transform);
        pad.transform.position = pos;
        pad.transform.localScale = new Vector3(4f, 0.05f, 1.2f);
        Destroy(pad.GetComponent<Collider>());
        Tint(pad, col);
        return pad;
    }

    static void Tint(GameObject go, Color col)
    {
        var mat = go.GetComponent<Renderer>().material;
        mat.SetColor("_BaseColor", col);
        mat.SetColor("_EmissiveColor", col * 0.8f);
        mat.color = col;
    }

    void BuildHud()
    {
        hud = UIFactory.CreateOverlayCanvas("DashHud", 60, transform);
        fuseLabel = UIFactory.Label(hud.transform, "20", 150, UIFactory.Parchment, new Vector2(0, 400));
        lootLabel = UIFactory.Label(hud.transform, "LOOT 0 / 0", 44, new Color(0.9f, 0.75f, 0.2f), new Vector2(0, 300));
        UIFactory.Label(hud.transform, "GRAB THE GOLD  ~  REACH THE GREEN BEFORE IT BLOWS", 30, UIFactory.Rust, new Vector2(0, -470));

        var panel = UIFactory.FullScreenPanel(hud.transform, new Color(0.08f, 0.05f, 0.04f, 0.92f));
        resultPanel = panel.gameObject;
        resultTitle = UIFactory.Label(resultPanel.transform, "", 150, UIFactory.Parchment, new Vector2(0, 200));
        resultSub = UIFactory.Label(resultPanel.transform, "", 48, UIFactory.Rust, new Vector2(0, 90));
        UIFactory.MenuButton(resultPanel.transform, "AGAIN", new Vector2(0, -40), Restart);
        UIFactory.MenuButton(resultPanel.transform, GameFlow.Instance != null ? GameFlow.Instance.MinigameExitLabel : "MENU", new Vector2(0, -150), Leave);
        resultPanel.SetActive(false);
    }

    void Update()
    {
        if (phase == Phase.Playing)
        {
            Tick();
        }
        if (shake > 0f)
        {
            ApplyShake();
        }
    }

    void Tick()
    {
        Move();
        SpinCoins();
        CollectLoot();

        fuse -= Time.deltaTime;
        int whole = Mathf.CeilToInt(Mathf.Max(0f, fuse));
        fuseLabel.text = whole.ToString();
        fuseLabel.color = fuse < 5f ? Color.Lerp(UIFactory.Rust, Color.white, Mathf.PingPong(Time.time * 6f, 1f)) : UIFactory.Parchment;
        if (whole < lastTick)
        {
            lastTick = whole;
            AudioManager.Instance?.PlaySfx("tick");
        }

        if (InExit())
        {
            End(true);
            return;
        }
        if (fuse <= 0f)
        {
            End(false);
        }
    }

    void Move()
    {
        var kb = Keyboard.current;
        if (kb == null)
        {
            return;
        }
        var dir = new Vector3(
            (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0),
            0,
            (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0));
        if (dir.sqrMagnitude > 1f)
        {
            dir.Normalize();
        }
        var p = player.position + dir * PlayerSpeed * Time.deltaTime;
        p.x = Mathf.Clamp(p.x, MinX, MaxX);
        p.z = Mathf.Clamp(p.z, MinZ, MaxZ);
        player.position = p;
    }

    void SpinCoins()
    {
        foreach (var c in coins)
            if (c.gameObject.activeSelf)
            {
                c.Rotate(0, 180f * Time.deltaTime, 0, Space.World);
            }
    }

    void CollectLoot()
    {
        foreach (var c in coins)
        {
            if (!c.gameObject.activeSelf)
            {
                continue;
            }
            if ((c.position - player.position).sqrMagnitude < 1f)
            {
                c.gameObject.SetActive(false);
                loot++;
                lootLabel.text = $"LOOT {loot} / {lootTotal}";
                AudioManager.Instance?.PlaySfx("coin");
            }
        }
    }

    bool InExit() => Mathf.Abs(player.position.x - exit.position.x) < 2.2f && player.position.z > MaxZ - 0.6f;

    void End(bool escaped)
    {
        phase = Phase.Over;
        resultPanel.SetActive(true);
        if (escaped)
        {
            int payout = loot * 100;
            Wallet.Add(payout);
            resultTitle.text = "ESCAPED";
            resultTitle.color = UIFactory.Parchment;
            resultSub.text = $"made off with ${payout}";
            AudioManager.Instance?.PlaySfx("win");
        }
        else
        {
            resultTitle.text = "BOOM";
            resultTitle.color = UIFactory.Rust;
            resultSub.text = "blown to bits";
            shake = 0.6f;
            AudioManager.Instance?.PlaySfx("explosion");
        }
    }

    void ApplyShake()
    {
        shake -= Time.deltaTime;
        var jitter = Random.insideUnitSphere * shake * 0.6f;
        cam.transform.position = new Vector3(0, 17f, -13f) + jitter;
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

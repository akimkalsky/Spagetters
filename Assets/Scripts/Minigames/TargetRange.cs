using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TargetRange : MonoBehaviour
{
    enum Phase { Playing, Over }

    const float RoundTime = 30f;
    const float MinX = -6.5f, MaxX = 6.5f;
    const int Clip = 6;

    class Bottle { public Transform t; public float vx; public float respawn; }

    Phase phase;
    float clock;
    int score, combo, bestCombo, shots, hits, ammo;
    bool reloading;
    int lastTick;

    Camera cam;
    readonly List<Bottle> bottles = new();

    Canvas hud;
    TMP_Text timeLabel, scoreLabel, comboLabel, ammoLabel;
    RectTransform crosshair;
    GameObject resultPanel;
    TMP_Text resultTitle, resultSub;

    void Awake() => GameFlow.Instance?.EnterMinigame();

    void Start()
    {
        BuildRange();
        BuildHud();
        Restart();
    }

    void Restart()
    {
        phase = Phase.Playing;
        clock = RoundTime;
        score = combo = bestCombo = shots = hits = 0;
        ammo = Clip;
        reloading = false;
        lastTick = Mathf.CeilToInt(RoundTime);
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
        foreach (var b in bottles) { b.t.gameObject.SetActive(true); b.respawn = 0f; }
        Cursor.visible = false;
        AudioManager.Instance?.PlayAmbient("wind", 0.15f);
        UpdateHud();
    }

    void BuildRange()
    {
        cam = Camera.main;
        if (cam == null)
        {
            cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
        }
        cam.transform.position = new Vector3(0, 1.6f, -8f);
        cam.transform.rotation = Quaternion.identity;

        var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
        back.name = "Backdrop";
        back.transform.SetParent(transform);
        back.transform.position = new Vector3(0, 1.4f, 1.2f);
        back.transform.localScale = new Vector3(18f, 6f, 0.4f);
        Destroy(back.GetComponent<Collider>());
        Tint(back, new Color(0.28f, 0.19f, 0.13f), 0.1f);

        var light = new GameObject("Sun").AddComponent<Light>();
        light.transform.SetParent(transform);
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(50f, -20f, 0f);

        float[] rows = { 0.7f, 1.5f, 2.3f };
        for (int r = 0; r < rows.Length; r++)
        {
            var shelf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelf.name = "Shelf";
            shelf.transform.SetParent(transform);
            shelf.transform.position = new Vector3(0, rows[r] - 0.28f, 0f);
            shelf.transform.localScale = new Vector3(15f, 0.12f, 0.6f);
            Destroy(shelf.GetComponent<Collider>());
            Tint(shelf, new Color(0.36f, 0.24f, 0.15f), 0.05f);

            int count = 5;
            for (int i = 0; i < count; i++)
            {
                float x = Mathf.Lerp(MinX, MaxX, count == 1 ? 0.5f : (float)i / (count - 1));
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "Bottle";
                go.transform.SetParent(transform);
                go.transform.position = new Vector3(x, rows[r], 0f);
                go.transform.localScale = new Vector3(0.28f, 0.5f, 0.28f);
                Tint(go, new Color(0.25f, 0.6f, 0.35f), 0.5f);
                bottles.Add(new Bottle { t = go.transform, vx = r == 1 ? (i % 2 == 0 ? 2.2f : -2.2f) : 0f });
            }
        }
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
        hud = UIFactory.CreateOverlayCanvas("RangeHud", 60, transform);
        timeLabel = UIFactory.Label(hud.transform, "30", 150, UIFactory.Parchment, new Vector2(0, 400));
        scoreLabel = UIFactory.Label(hud.transform, "0", 90, new Color(0.9f, 0.75f, 0.2f), new Vector2(-760, 400));
        comboLabel = UIFactory.Label(hud.transform, "", 54, UIFactory.Rust, new Vector2(0, 300));
        ammoLabel = UIFactory.Label(hud.transform, "", 60, UIFactory.Parchment, new Vector2(0, -440));

        crosshair = UIFactory.Label(hud.transform, "+", 90, UIFactory.Rust, Vector2.zero).rectTransform;

        var panel = UIFactory.FullScreenPanel(hud.transform, new Color(0.08f, 0.05f, 0.04f, 0.92f));
        resultPanel = panel.gameObject;
        resultTitle = UIFactory.Label(resultPanel.transform, "TIME", 150, UIFactory.Parchment, new Vector2(0, 200));
        resultSub = UIFactory.Label(resultPanel.transform, "", 48, UIFactory.Rust, new Vector2(0, 90));
        UIFactory.MenuButton(resultPanel.transform, "AGAIN", new Vector2(0, -40), Restart);
        UIFactory.MenuButton(resultPanel.transform, "MENU", new Vector2(0, -150), Leave);
        resultPanel.SetActive(false);
    }

    void Update()
    {
        if (phase == Phase.Playing)
        {
            Tick();
        }
    }

    void Tick()
    {
        AimCrosshair();
        MoveBottles();
        RespawnBottles();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Fire();
        }
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            StartReload();
        }

        clock -= Time.deltaTime;
        int whole = Mathf.CeilToInt(Mathf.Max(0f, clock));
        timeLabel.text = whole.ToString();
        timeLabel.color = clock < 5f ? Color.Lerp(UIFactory.Rust, Color.white, Mathf.PingPong(Time.time * 6f, 1f)) : UIFactory.Parchment;
        if (whole < lastTick)
        {
            lastTick = whole;
            AudioManager.Instance?.PlaySfx("tick");
        }
        if (clock <= 0f)
        {
            End();
        }
    }

    void AimCrosshair()
    {
        var mouse = Mouse.current;
        Vector2 mp = mouse != null ? mouse.position.ReadValue() : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        crosshair.anchoredPosition = new Vector2(
            (mp.x / Screen.width - 0.5f) * 1920f,
            (mp.y / Screen.height - 0.5f) * 1080f);
    }

    void Fire()
    {
        if (reloading)
        {
            return;
        }
        if (ammo <= 0)
        {
            AudioManager.Instance?.PlaySfx("empty");
            StartReload();
            return;
        }

        ammo--;
        shots++;
        AudioManager.Instance?.PlaySfx("gunshot", 0.7f);

        var ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out var hit, 50f) && hit.transform.name == "Bottle" && hit.transform.gameObject.activeSelf)
        {
            Pop(hit.transform);
        }
        else
        {
            combo = 0;
        }

        UpdateHud();
        if (ammo <= 0)
        {
            StartReload();
        }
    }

    void Pop(Transform t)
    {
        hits++;
        combo++;
        bestCombo = Mathf.Max(bestCombo, combo);
        score += 10 * Mathf.Max(1, combo);
        var b = bottles.Find(x => x.t == t);
        if (b != null)
        {
            b.respawn = Random.Range(0.8f, 1.8f);
        }
        t.gameObject.SetActive(false);
        AudioManager.Instance?.PlaySfx("shatter");
        StartCoroutine(Shards(t.position));
    }

    IEnumerator Shards(Vector3 at)
    {
        var shards = new List<Transform>();
        var vel = new List<Vector3>();
        for (int i = 0; i < 6; i++)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(s.GetComponent<Collider>());
            s.transform.SetParent(transform);
            s.transform.position = at;
            s.transform.localScale = Vector3.one * 0.1f;
            Tint(s, new Color(0.25f, 0.6f, 0.35f), 0.5f);
            shards.Add(s.transform);
            vel.Add(new Vector3(Random.Range(-2f, 2f), Random.Range(1f, 4f), Random.Range(-1f, 1f)));
        }
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            for (int i = 0; i < shards.Count; i++)
            {
                vel[i] += Vector3.down * 9f * Time.deltaTime;
                shards[i].position += vel[i] * Time.deltaTime;
                shards[i].Rotate(200f * Time.deltaTime, 0, 120f * Time.deltaTime);
            }
            yield return null;
        }
        foreach (var s in shards) if (s != null)
        {
            Destroy(s.gameObject);
        }
    }

    void MoveBottles()
    {
        foreach (var b in bottles)
        {
            if (b.vx == 0f || !b.t.gameObject.activeSelf)
            {
                continue;
            }
            var p = b.t.position;
            p.x += b.vx * Time.deltaTime;
            if (p.x < MinX || p.x > MaxX)
            {
                b.vx = -b.vx;
                p.x = Mathf.Clamp(p.x, MinX, MaxX);
            }
            b.t.position = p;
        }
    }

    void RespawnBottles()
    {
        foreach (var b in bottles)
        {
            if (b.t.gameObject.activeSelf || b.respawn <= 0f)
            {
                continue;
            }
            b.respawn -= Time.deltaTime;
            if (b.respawn <= 0f)
            {
                if (b.vx != 0f)
                {
                    var p = b.t.position; p.x = Random.Range(MinX, MaxX); b.t.position = p;
                }
                b.t.gameObject.SetActive(true);
            }
        }
    }

    void StartReload()
    {
        if (reloading || ammo == Clip)
        {
            return;
        }
        StartCoroutine(Reload());
    }

    IEnumerator Reload()
    {
        reloading = true;
        ammoLabel.text = "RELOADING";
        yield return new WaitForSeconds(1f);
        ammo = Clip;
        reloading = false;
        UpdateHud();
    }

    void UpdateHud()
    {
        scoreLabel.text = score.ToString();
        comboLabel.text = combo > 1 ? $"x{combo}" : "";
        if (!reloading)
        {
            ammoLabel.text = new string('●', ammo) + new string('○', Clip - ammo);
        }
    }

    void End()
    {
        phase = Phase.Over;
        Cursor.visible = true;
        resultPanel.SetActive(true);

        string medal = score >= 600 ? "DEAD-EYE" : score >= 300 ? "SHARPSHOOTER" : "GREENHORN";
        int best = PlayerPrefs.GetInt("range_best", 0);
        bool newBest = score > best;
        if (newBest)
        {
            PlayerPrefs.SetInt("range_best", score);
            best = score;
        }

        Wallet.Add(score);
        resultTitle.text = medal;
        resultTitle.color = score >= 300 ? UIFactory.Parchment : UIFactory.Rust;
        int acc = shots > 0 ? Mathf.RoundToInt(100f * hits / shots) : 0;
        string tag = newBest ? "NEW BEST!" : $"best {best}";
        resultSub.text = $"score {score}   {acc}% accuracy   x{Mathf.Max(1, bestCombo)} combo   +${score}   {tag}";
        AudioManager.Instance?.PlaySfx(score >= 300 ? "win" : "lose");
    }

    void Leave()
    {
        Cursor.visible = true;
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

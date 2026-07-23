using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BankCrack : MonoBehaviour
{
    enum Phase { Playing, Over }

    const int Positions = 40;
    const int ComboLen = 3;
    const float AlarmTime = 25f;
    const float SpinSpeed = 130f;
    const float StepDeg = 360f / Positions;

    Phase phase;
    float alarm;
    float angle;
    int locked;
    int lastTick;
    readonly int[] combo = new int[ComboLen];

    Canvas canvas;
    RectTransform dial;
    TMP_Text numberLabel, comboLabel, alarmLabel;
    GameObject resultPanel;
    TMP_Text resultTitle, resultSub;
    static Sprite dialSprite;

    int CurrentNumber => ((Mathf.RoundToInt(angle / StepDeg) % Positions) + Positions) % Positions;

    void Awake() => GameFlow.Instance?.EnterMinigame();

    void Start()
    {
        Build();
        Restart();
    }

    void Restart()
    {
        phase = Phase.Playing;
        alarm = AlarmTime;
        angle = 0f;
        locked = 0;
        lastTick = Mathf.CeilToInt(AlarmTime);
        NewCombo();
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
        AudioManager.Instance?.PlayAmbient("wind", 0.12f);
        UpdateLabels();
    }

    void NewCombo()
    {
        for (int i = 0; i < ComboLen; i++)
        {
            int n;
            bool dup;
            do
            {
                n = Random.Range(0, Positions);
                dup = false;
                for (int j = 0; j < i; j++)
                {
                    if (combo[j] == n)
                    {
                        dup = true;
                    }
                }
            }
            while (dup);
            combo[i] = n;
        }
    }

    void Build()
    {
        canvas = UIFactory.CreateOverlayCanvas("BankHud", 60, transform);
        UIFactory.FullScreenPanel(canvas.transform, new Color(0.10f, 0.07f, 0.05f, 1f));
        alarmLabel = UIFactory.Label(canvas.transform, "25", 150, UIFactory.Parchment, new Vector2(0, 400));
        UIFactory.Label(canvas.transform, "A / D  SPIN THE DIAL      SPACE  SET", 30, UIFactory.Rust, new Vector2(0, -470));

        var dialImg = new GameObject("Dial").AddComponent<Image>();
        dialImg.transform.SetParent(canvas.transform, false);
        dialImg.sprite = DialSprite();
        dial = dialImg.rectTransform;
        dial.sizeDelta = new Vector2(420, 420);
        dial.anchoredPosition = new Vector2(0, -30);

        var pointer = UIFactory.Label(canvas.transform, "▼", 60, UIFactory.Rust, new Vector2(0, 210));
        pointer.name = "Pointer";

        numberLabel = UIFactory.Label(canvas.transform, "0", 120, UIFactory.Parchment, new Vector2(0, -30));
        comboLabel = UIFactory.Label(canvas.transform, "", 54, UIFactory.Parchment, new Vector2(0, 250));

        var panel = UIFactory.FullScreenPanel(canvas.transform, new Color(0.08f, 0.05f, 0.04f, 0.92f));
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

        int before = CurrentNumber;
        var kb = Keyboard.current;
        if (kb != null)
        {
            float dir = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                      - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
            angle += dir * SpinSpeed * Time.deltaTime;
            dial.localRotation = Quaternion.Euler(0, 0, -angle);
            if (kb.spaceKey.wasPressedThisFrame)
            {
                TrySet();
            }
        }

        if (CurrentNumber != before)
        {
            numberLabel.text = CurrentNumber.ToString();
            AudioManager.Instance?.PlaySfx("tick", 0.4f);
        }

        alarm -= Time.deltaTime;
        int whole = Mathf.CeilToInt(Mathf.Max(0f, alarm));
        alarmLabel.text = whole.ToString();
        alarmLabel.color = alarm < 5f ? Color.Lerp(UIFactory.Rust, Color.white, Mathf.PingPong(Time.time * 6f, 1f)) : UIFactory.Parchment;
        if (whole < lastTick)
        {
            lastTick = whole;
            AudioManager.Instance?.PlaySfx("tick");
        }
        if (alarm <= 0f)
        {
            End(false);
        }
    }

    void TrySet()
    {
        if (CurrentNumber == combo[locked])
        {
            locked++;
            AudioManager.Instance?.PlaySfx("clunk");
            UpdateLabels();
            if (locked >= ComboLen)
            {
                End(true);
            }
        }
        else
        {
            AudioManager.Instance?.PlaySfx("alarm");
            alarm -= 2f;
        }
    }

    void UpdateLabels()
    {
        numberLabel.text = CurrentNumber.ToString();
        var parts = new string[ComboLen];
        for (int i = 0; i < ComboLen; i++)
            parts[i] = i < locked ? $"[{combo[i]:00}]" : $"{combo[i]:00}";
        comboLabel.text = string.Join("   ", parts);
    }

    void End(bool cracked)
    {
        phase = Phase.Over;
        resultPanel.SetActive(true);
        if (cracked)
        {
            Wallet.Add(500);
        }
        resultTitle.text = cracked ? "CRACKED" : "ALARM!";
        resultTitle.color = cracked ? UIFactory.Parchment : UIFactory.Rust;
        resultSub.text = cracked ? "the vault gives up $500" : "the law is on its way";
        AudioManager.Instance?.PlaySfx(cracked ? "win" : "lose");
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

    static Sprite DialSprite()
    {
        if (dialSprite != null)
        {
            return dialSprite;
        }
        int size = 256;
        float c = size * 0.5f, R = size * 0.47f, r = size * 0.30f, hub = size * 0.10f;
        var metal = new Color(0.55f, 0.55f, 0.62f);
        var dark = new Color(0.2f, 0.2f, 0.24f);
        var tick = new Color(0.85f, 0.85f, 0.92f);
        dialSprite = ProceduralTex.Generate(size, size, (x, y) =>
        {
            float dx = x - c, dy = y - c;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist <= hub)
            {
                return dark;
            }
            if (dist <= R && dist >= r)
            {
                float a = Mathf.Atan2(dy, dx) / (2f * Mathf.PI) * Positions;
                float frac = a - Mathf.Floor(a);
                if (dist >= R * 0.86f && (frac < 0.08f || frac > 0.92f))
                {
                    return tick;
                }
                return metal;
            }
            return Color.clear;
        });
        return dialSprite;
    }
}

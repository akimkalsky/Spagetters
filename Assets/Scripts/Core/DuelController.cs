using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DuelController : MonoBehaviour
{
    public enum Phase { Idle, Approach, Standoff, Draw, Resolve, Done }

    [Header("Pacing")]
    public int paceCount = 10;
    public float paceInterval = 0.42f;
    public float standoffHold = 0.6f;

    [Header("Draw window")]
    public float cueDelayMin = 0.3f;
    public float cueDelayMax = 1.5f;
    public float timeout = 1.2f;
    public bool tieGoesToPlayer = true;
    public bool feint = true;
    public float feintChance = 0.5f;

    [Header("Actors (optional, swap procedural for real later)")]
    public Transform playerActor;
    public Transform rivalActor;
    public SpriteAnimation playerAnim;
    public SpriteAnimation rivalAnim;
    public float paceStep = 0.55f;

    [Header("Juice")]
    public bool bulletTime = true;
    public float bulletTimeScale = 0.28f;
    public float bulletTimeRampIn = 0.08f;
    public float hitStopSeconds = 0.07f;
    public float shakeMagnitude = 0.35f;
    public float shakeSeconds = 0.35f;
    public float fovKick = 8f;
    public bool rumble = true;
    public float standoffZoom = 6f;
    public bool screenFlash = true;
    public float knockback = 0.9f;

    public Phase Current { get; private set; } = Phase.Idle;
    public float LastPlayerReaction { get; private set; } = -1f;
    public float LastMargin { get; private set; } = -1f;
    public float LastMarginFraction { get; private set; }
    public bool LastWon { get; private set; }

    public bool OwnsCountdown => Current == Phase.Approach || Current == Phase.Standoff || Current == Phase.Draw;

    public int? CueSeed;
    public bool ForcePerfectDraw;

    Coroutine run;
    Coroutine timeFx;
    float cueTime = -1f;
    bool fireLatched;
    Vector3 playerHome, rivalHome, paceAxis = Vector3.right;
    Camera cam;
    float baseFov = 60f;
    static Image flashImg;

    public static DuelController Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public static DuelController EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }
        var found = FindFirstObjectByType<DuelController>();
        Instance = found != null ? found : new GameObject("DuelController").AddComponent<DuelController>();
        return Instance;
    }

    public void BeginDuel()
    {
        if (Current != Phase.Idle && Current != Phase.Done)
        {
            return;
        }
        if (run != null)
        {
            StopCoroutine(run);
        }
        run = StartCoroutine(RunDuel());
    }

    public void SubmitFire()
    {
        fireLatched = true;
    }

    bool FirePressedThisFrame()
    {
        if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame))
        {
            return true;
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            return true;
        }
        return false;
    }

    bool ConsumeFire()
    {
        if (fireLatched || FirePressedThisFrame())
        {
            fireLatched = false;
            return true;
        }
        return false;
    }

    IEnumerator RunDuel()
    {
        ResetState();

        yield return null;

        Current = Phase.Approach;
        for (int n = paceCount; n > 0; n--)
        {
            GameEvents.RaisePace(n);
            StepActorsApart(paceCount - n + 1);
            float t = 0f;
            while (t < paceInterval)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        GameEvents.RaisePace(0);

        Current = Phase.Standoff;
        fireLatched = false;
        GameEvents.RaiseSteadyShown();
        var standoffCam = Cam();
        float hold = 0f;
        while (hold < standoffHold)
        {
            hold += Time.unscaledDeltaTime;
            if (standoffCam != null)
            {
                standoffCam.fieldOfView = Mathf.Lerp(baseFov, baseFov - standoffZoom, hold / standoffHold);
            }
            if (ConsumeFire())
            {
                yield return FalseStart();
                yield break;
            }
            yield return null;
        }

        Current = Phase.Draw;
        var rng = CueSeed.HasValue ? new System.Random(CueSeed.Value) : new System.Random();
        float cueDelay = Mathf.Lerp(cueDelayMin, cueDelayMax, (float)rng.NextDouble());

        float feintAt = -1f;
        if (FeintEnabled() && rng.NextDouble() < feintChance && cueDelay > 0.55f)
        {
            feintAt = cueDelay * (0.3f + 0.35f * (float)rng.NextDouble());
        }

        float waited = 0f;
        bool feinted = false;
        while (waited < cueDelay)
        {
            waited += Time.unscaledDeltaTime;
            if (!feinted && feintAt > 0f && waited >= feintAt)
            {
                feinted = true;
                GameEvents.RaiseFeintFlashed();
            }
            if (ConsumeFire())
            {
                yield return FalseStart();
                yield break;
            }
            yield return null;
        }

        cueTime = Time.unscaledTime;
        fireLatched = false;
        GameEvents.RaiseDrawWindowOpened();
        if (bulletTime)
        {
            if (timeFx != null)
            {
                StopCoroutine(timeFx);
            }
            timeFx = StartCoroutine(BulletTimeRamp());
        }

        Current = Phase.Resolve;
        float aiReaction = CurrentAiReaction();
        float elapsed = 0f;
        bool pressed = false;
        while (elapsed < timeout)
        {
            if (ForcePerfectDraw || ConsumeFire())
            {
                pressed = true;
                break;
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!pressed)
        {
            yield return Finish(false, -1f, rivalWins: true);
            yield break;
        }

        float playerReaction = ForcePerfectDraw ? 0f : Time.unscaledTime - cueTime;
        bool won = ResolveWin(playerReaction, aiReaction, tieGoesToPlayer);
        LastMargin = aiReaction - playerReaction;
        LastMarginFraction = aiReaction > 0f ? Mathf.Clamp01(LastMargin / aiReaction) : 0f;
        yield return Finish(won, playerReaction, rivalWins: !won);
    }

    IEnumerator FalseStart()
    {
        Current = Phase.Resolve;
        yield return Finish(false, -1f, rivalWins: true);
    }

    IEnumerator Finish(bool won, float reaction, bool rivalWins)
    {
        Current = Phase.Done;
        LastWon = won;
        LastPlayerReaction = reaction;
        GameEvents.RaiseShot();

        if (timeFx != null)
        {
            StopCoroutine(timeFx);
            timeFx = null;
        }

        var shooter = won ? playerActor : rivalActor;
        var victim = won ? rivalActor : playerActor;
        var shooterAnim = won ? playerAnim : rivalAnim;
        var victimAnim = won ? rivalAnim : playerAnim;
        Vector3 shotDir = won ? paceAxis : -paceAxis;

        if (shooterAnim != null)
        {
            shooterAnim.PlayAction();
        }
        if (shooter != null)
        {
            StartCoroutine(ScalePunch(shooter));
            StartCoroutine(Recoil(shooter, -shotDir));
        }
        if (victimAnim != null && victimAnim.spriteRenderer != null)
        {
            StartCoroutine(HitFlash(victimAnim.spriteRenderer));
        }
        if (!won && victim != null)
        {
            StartCoroutine(Recoil(victim, shotDir));
        }
        if (rumble)
        {
            StartCoroutine(Rumble(0.09f));
        }
        if (screenFlash && !Accessibility.ReduceFlashing)
        {
            StartCoroutine(ScreenFlash(won ? new Color(1f, 0.95f, 0.8f) : new Color(0.85f, 0.08f, 0.04f), 0.3f));
        }

        if (hitStopSeconds > 0f)
        {
            yield return HitStop(hitStopSeconds);
        }
        Time.timeScale = 1f;

        Camera c = Cam();
        if (c != null && !Accessibility.ReduceFlashing)
        {
            StartCoroutine(FovKick(c));
            StartCoroutine(CameraPunch(c));
        }

        bool fatal = !won && GameFlow.Instance != null && GameFlow.Instance.duelLossIsFatal;

        if (won && victim != null)
        {
            yield return DeathFall(victim, shotDir);
        }
        else if (fatal)
        {
            yield return DeathSequence();
        }
        else
        {
            yield return new WaitForSecondsRealtime(0.25f);
        }
        if (c != null)
        {
            c.fieldOfView = baseFov;
        }

        GameEvents.RaiseDuelEnded(won, reaction);
    }

    IEnumerator DeathSequence()
    {
        var img = FlashImage();
        if (img == null)
        {
            yield return new WaitForSecondsRealtime(1.4f);
            yield break;
        }

        Time.timeScale = 0.32f;
        var blood = new Color(0.42f, 0f, 0f);

        float up = 1.3f;
        float t = 0f;
        while (t < up)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.SmoothStep(0f, 0.93f, Mathf.Pow(t / up, 0.7f));
            img.color = new Color(blood.r, blood.g, blood.b, a);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.4f);

        float down = 0.45f;
        t = 0f;
        while (t < down)
        {
            t += Time.unscaledDeltaTime;
            img.color = new Color(blood.r, blood.g, blood.b, Mathf.Lerp(0.93f, 0f, t / down));
            yield return null;
        }
        img.color = new Color(0f, 0f, 0f, 0f);
        Time.timeScale = 1f;
    }

    public static bool ResolveWin(float playerReaction, float aiReaction, bool tieToPlayer)
    {
        return tieToPlayer ? playerReaction <= aiReaction : playerReaction < aiReaction;
    }

    public static float AiReaction(float rivalReaction, float drawAdvantage)
    {
        return Mathf.Max(0.05f, rivalReaction + drawAdvantage);
    }

    float CurrentAiReaction()
    {
        var r = RivalRoster.Current;
        return AiReaction(r != null ? r.Reaction : 0.35f, Loadout.DrawAdvantage);
    }

    bool FeintEnabled()
    {
        if (!feint)
        {
            return false;
        }
        var r = RivalRoster.Current;
        return r != null && r.Index >= RivalRoster.Count - 2;
    }

    void ResetState()
    {
        fireLatched = false;
        cueTime = -1f;
        LastPlayerReaction = -1f;
        LastMargin = -1f;
        LastMarginFraction = 0f;
        LastWon = false;
        Time.timeScale = 1f;

        if (playerActor != null)
        {
            playerHome = playerActor.position;
        }
        if (rivalActor != null)
        {
            rivalHome = rivalActor.position;
        }

        paceAxis = Vector3.right;
        if (playerActor != null && rivalActor != null)
        {
            Vector3 sep = rivalHome - playerHome;
            sep.y = 0f;
            if (sep.sqrMagnitude > 0.0001f)
            {
                paceAxis = sep.normalized;
            }
        }

        var c0 = Cam();
        if (c0 != null)
        {
            baseFov = c0.fieldOfView;
        }
    }

    void StepActorsApart(int step)
    {
        float d = step * paceStep;
        if (playerActor != null)
        {
            playerActor.position = playerHome - paceAxis * d;
        }
        if (rivalActor != null)
        {
            rivalActor.position = rivalHome + paceAxis * d;
        }
    }

    Camera Cam()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }
        return cam;
    }

    IEnumerator BulletTimeRamp()
    {
        float from = Time.timeScale;
        float t = 0f;
        while (t < bulletTimeRampIn)
        {
            t += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(from, bulletTimeScale, t / bulletTimeRampIn);
            yield return null;
        }
        Time.timeScale = bulletTimeScale;
        while (true)
        {
            yield return null;
        }
    }

    IEnumerator HitStop(float seconds)
    {
        Time.timeScale = 0f;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    IEnumerator CameraPunch(Camera c)
    {
        Vector3 homePos = c.transform.localPosition;
        Quaternion homeRot = c.transform.localRotation;
        float t = 0f;
        while (t < shakeSeconds)
        {
            t += Time.unscaledDeltaTime;
            float falloff = 1f - (t / shakeSeconds);
            Vector2 o = UnityEngine.Random.insideUnitCircle * shakeMagnitude * falloff;
            float roll = (UnityEngine.Random.value - 0.5f) * 2f * shakeMagnitude * 8f * falloff;
            c.transform.localPosition = homePos + new Vector3(o.x, o.y, 0f);
            c.transform.localRotation = homeRot * Quaternion.Euler(0f, 0f, roll);
            yield return null;
        }
        c.transform.localPosition = homePos;
        c.transform.localRotation = homeRot;
    }

    IEnumerator FovKick(Camera c)
    {
        float dur = 0.25f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            c.fieldOfView = baseFov - fovKick * (1f - t / dur);
            yield return null;
        }
        c.fieldOfView = baseFov;
    }

    IEnumerator ScalePunch(Transform tr)
    {
        Vector3 home = tr.localScale;
        float dur = 0.18f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Sin((t / dur) * Mathf.PI);
            tr.localScale = home * (1f + 0.25f * k);
            yield return null;
        }
        tr.localScale = home;
    }

    IEnumerator HitFlash(SpriteRenderer sr)
    {
        Color home = sr.color;
        var hit = new Color(1f, 0.2f, 0.15f, home.a);
        for (int i = 0; i < 3; i++)
        {
            sr.color = hit;
            yield return new WaitForSecondsRealtime(0.06f);
            sr.color = home;
            yield return new WaitForSecondsRealtime(0.05f);
        }
        sr.color = home;
    }

    IEnumerator Rumble(float seconds)
    {
        var pad = Gamepad.current;
        if (pad == null)
        {
            yield break;
        }
        pad.SetMotorSpeeds(0.35f, 0.7f);
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        pad.SetMotorSpeeds(0f, 0f);
    }

    IEnumerator ScreenFlash(Color color, float seconds)
    {
        var img = FlashImage();
        if (img == null)
        {
            yield break;
        }
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = t / seconds;
            float a = Mathf.Pow(1f - k, 2.5f);
            Color c = Color.Lerp(Color.white, color, Mathf.Clamp01(k * 3f));
            img.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
        img.color = new Color(0f, 0f, 0f, 0f);
    }

    static Image FlashImage()
    {
        if (flashImg != null)
        {
            return flashImg;
        }
        var go = new GameObject("DuelFlashCanvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        var imgGo = new GameObject("Flash");
        imgGo.transform.SetParent(go.transform, false);
        flashImg = imgGo.AddComponent<Image>();
        flashImg.raycastTarget = false;
        var rt = flashImg.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        flashImg.color = new Color(1f, 1f, 1f, 0f);
        return flashImg;
    }

    IEnumerator DeathFall(Transform victim, Vector3 dir)
    {
        Vector3 startPos = victim.position;
        Quaternion startRot = victim.rotation;
        Vector3 tipAxis = Vector3.Cross(Vector3.up, dir).normalized;
        if (tipAxis.sqrMagnitude < 0.001f)
        {
            tipAxis = Vector3.right;
        }
        Quaternion endRot = Quaternion.AngleAxis(90f, tipAxis) * startRot;

        float dur = 0.5f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            victim.position = startPos + dir * (knockback * k);
            victim.rotation = Quaternion.Slerp(startRot, endRot, k * k);
            yield return null;
        }
        victim.position = startPos + dir * knockback;
        victim.rotation = endRot;
    }

    IEnumerator Recoil(Transform who, Vector3 dir)
    {
        Vector3 home = who.position;
        float dur = 0.16f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Sin((t / dur) * Mathf.PI);
            who.position = home + dir * (0.28f * k);
            yield return null;
        }
        who.position = home;
    }

    void OnDisable()
    {
        Gamepad.current?.SetMotorSpeeds(0f, 0f);
    }
}

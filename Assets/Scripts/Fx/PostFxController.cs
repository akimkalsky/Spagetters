using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class PostFxController : MonoBehaviour
{
    Vignette vignette;
    FilmGrain grain;

    void Start()
    {
        Build();
        GameFlow.Instance.StateChanged += Apply;
        Accessibility.Changed += Reapply;
        Apply(GameFlow.Instance.State);
    }

    void OnDestroy()
    {
        if (GameFlow.Instance != null)
        {
            GameFlow.Instance.StateChanged -= Apply;
        }
        Accessibility.Changed -= Reapply;
    }

    void Reapply() => Apply(GameFlow.Instance.State);

    static readonly Color VigWarm = new Color(0.09f, 0.04f, 0.02f);
    static readonly Color VigCool = new Color(0.02f, 0.03f, 0.08f);
    static readonly Color VigNeutral = new Color(0.05f, 0.03f, 0.02f);

    float baseVig = 0.45f;

    void Update()
    {
        if (vignette == null)
        {
            return;
        }
        bool menu = GameFlow.Instance != null && GameFlow.Instance.State == GameState.MainMenu;
        bool run = RunClock.Instance != null && RunClock.Instance.Running;
        float night = DayNightClock.Instance != null ? DayNightClock.Instance.Night : 0f;

        vignette.color.value = menu || run ? Color.Lerp(VigWarm, VigCool, night) : VigNeutral;
        vignette.intensity.value = run ? Mathf.Lerp(baseVig, 0.5f, night * 0.6f) : baseVig;
    }

    void Build()
    {
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();

        vignette = profile.Add<Vignette>(true);
        vignette.color.value = new Color(0.05f, 0.03f, 0.02f);
        vignette.smoothness.value = 0.5f;

        grain = profile.Add<FilmGrain>(true);
        grain.type.value = FilmGrainLookup.Medium1;
        grain.response.value = 0.8f;

        var motionBlur = profile.Add<MotionBlur>(true);
        motionBlur.intensity.value = 0f;

        var v = gameObject.AddComponent<Volume>();
        v.isGlobal = true;
        v.priority = 100f;
        v.sharedProfile = profile;
    }

    void Apply(GameState s)
    {
        bool framed = s == GameState.MainMenu || s == GameState.Result || s == GameState.Paused;
        baseVig = framed ? 0.45f : 0.2f;
        float g = framed ? 0.35f : 0.12f;
        grain.intensity.value = Accessibility.ReduceFlashing ? Mathf.Min(g, 0.1f) : g;
    }
}

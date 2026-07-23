using UnityEngine;

public class DayNightClock : MonoBehaviour
{
    public static DayNightClock Instance { get; private set; }

    public float cycleSeconds = 90f;
    public float Phase01 { get; private set; } = 0.28f;

    public float Night => RunClock.Instance != null && RunClock.Instance.Running
        ? RunClock.Instance.NightProgress
        : AmbientNight;

    float AmbientNight => (1f - Mathf.Cos(Phase01 * 2f * Mathf.PI)) * 0.5f;

    void Awake() => Instance = this;

    void Update() => Phase01 = Mathf.Repeat(Phase01 + Time.unscaledDeltaTime / cycleSeconds, 1f);
}

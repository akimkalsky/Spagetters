using UnityEngine;

public class SundownLighting : MonoBehaviour
{
    public Light sun;
    public float dayPitch = 55f;
    public float duskPitch = 3f;

    static readonly Color Day = new Color(1f, 0.96f, 0.86f);
    static readonly Color Dusk = new Color(1f, 0.5f, 0.22f);
    static readonly Color Dark = new Color(0.32f, 0.34f, 0.5f);

    float baseYaw = -30f;

    void Start()
    {
        if (sun == null)
        {
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional)
                {
                    sun = l;
                    break;
                }
            }
        }
        if (sun != null)
        {
            baseYaw = sun.transform.eulerAngles.y;
        }
    }

    void Update()
    {
        if (sun == null)
        {
            return;
        }
        float night = DayNightClock.Instance != null ? DayNightClock.Instance.Night : 0f;
        sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(dayPitch, duskPitch, night), baseYaw, 0f);
        sun.color = night < 0.6f
            ? Color.Lerp(Day, Dusk, night / 0.6f)
            : Color.Lerp(Dusk, Dark, (night - 0.6f) / 0.4f);
    }
}

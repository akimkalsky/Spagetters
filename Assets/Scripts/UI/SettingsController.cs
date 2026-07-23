using UnityEngine;

public class SettingsController : MonoBehaviour
{
    public static SettingsController Instance { get; private set; }

    Canvas canvas;

    const string KMaster = "vol_master", KSfx = "vol_sfx", KMusic = "vol_music";
    const string KFull = "fullscreen", KFlash = "reduce_flash";

    void Awake() => Instance = this;

    void Start()
    {
        Apply();
        Build();
    }

    void Apply()
    {
        var a = AudioManager.Instance;
        a?.SetMasterVolume(PlayerPrefs.GetFloat(KMaster, 1f));
        a?.SetSfxVolume(PlayerPrefs.GetFloat(KSfx, 1f));
        a?.SetMusicVolume(PlayerPrefs.GetFloat(KMusic, 1f));
        Screen.fullScreen = PlayerPrefs.GetInt(KFull, 1) == 1;
        Accessibility.SetReduceFlashing(PlayerPrefs.GetInt(KFlash, 0) == 1);
    }

    public void Open() => canvas.gameObject.SetActive(true);
    public void Close() => canvas.gameObject.SetActive(false);

    void Build()
    {
        canvas = UIFactory.CreateOverlayCanvas("SettingsCanvas", 210, transform);
        UIFactory.FullScreenPanel(canvas.transform, new Color(0.08f, 0.05f, 0.04f, 1f));
        UIFactory.Label(canvas.transform, "SETTINGS", 96, UIFactory.Parchment, new Vector2(0, 340));

        UIFactory.Slider(canvas.transform, "MASTER", PlayerPrefs.GetFloat(KMaster, 1f), new Vector2(0, 190),
            v => Save(KMaster, v, () => AudioManager.Instance?.SetMasterVolume(v)));
        UIFactory.Slider(canvas.transform, "SFX", PlayerPrefs.GetFloat(KSfx, 1f), new Vector2(0, 110),
            v => Save(KSfx, v, () => { AudioManager.Instance?.SetSfxVolume(v); AudioManager.Instance?.PlaySfx("ui_move"); }));
        UIFactory.Slider(canvas.transform, "MUSIC", PlayerPrefs.GetFloat(KMusic, 1f), new Vector2(0, 30),
            v => Save(KMusic, v, () => AudioManager.Instance?.SetMusicVolume(v)));

        UIFactory.Toggle(canvas.transform, "FULLSCREEN", PlayerPrefs.GetInt(KFull, 1) == 1, new Vector2(0, -60),
            on => SaveInt(KFull, on, () => Screen.fullScreen = on));
        UIFactory.Toggle(canvas.transform, "REDUCE FLASHING", PlayerPrefs.GetInt(KFlash, 0) == 1, new Vector2(0, -140),
            on => SaveInt(KFlash, on, () => Accessibility.SetReduceFlashing(on)));

        UIFactory.MenuButton(canvas.transform, "BACK", new Vector2(0, -280), Close);
        canvas.gameObject.SetActive(false);
    }

    void Save(string key, float v, System.Action apply)
    {
        PlayerPrefs.SetFloat(key, v);
        apply();
    }

    void SaveInt(string key, bool v, System.Action apply)
    {
        PlayerPrefs.SetInt(key, v ? 1 : 0);
        apply();
    }
}

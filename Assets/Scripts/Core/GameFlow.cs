using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public enum GameState { Boot, MainMenu, Explore, Duel, Paused, Result, Minigame }

public class GameFlow : MonoBehaviour
{
    public static GameFlow Instance { get; private set; }

    public const string DefaultDuelScene = "OutdoorsScene";
    public const string MenuScene = "";

    public static string DuelScene => "Outdoors_Test";

    // public static string DuelScene
    // {
    //     get
    //     {
    // #if UNITY_EDITOR
    //         return PlayerPrefs.GetString("dev.duelScene", DefaultDuelScene);
    // #else
    //         return DefaultDuelScene;
    // #endif
    //     }
    // }

    [Header("Open world")]
    public int startingSteps = 50;
    public bool duelLossIsFatal = true;
    public int duelLossPenalty = 6;

    [Header("Side job reward (steps scale with the minigame payout)")]
    public int minigameBaseSteps = 4;
    public float minigameStepsPerCoin = 0.03f;
    public int minigameMaxSteps = 30;

    public GameState State { get; private set; } = GameState.Boot;
    public event Action<GameState> StateChanged;

    public bool PlayerWon { get; private set; }
    public float LastReaction { get; private set; } = -1f;
    public bool LastWasCoward { get; private set; }

    public Goon activeGoon;
    GameState prePause = GameState.Explore;
    bool minigameFromWorld;
    JobStation pendingJob;
    int winStreak;
    int minigameEarnedBefore;
    Scene worldScene;
    readonly List<GameObject> hiddenRoots = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }
        new GameObject("~GameFlow").AddComponent<GameFlow>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Spawn<ScreenFader>("ScreenFader");
        Spawn<AudioManager>("AudioManager");
        Spawn<GameAudio>("GameAudio");
        Spawn<RunClock>("RunClock");
        Spawn<PostFxController>("PostFx");
        Spawn<DayNightClock>("DayNight");
        Spawn<TitleBackground>("TitleBg");
        Spawn<RivalSelectController>("RivalSelectUI");
        Spawn<MinigameSelectController>("MinigameSelectUI");
        Spawn<StoreController>("StoreUI");
        Spawn<MainMenuController>("MainMenuUI");
        Spawn<HUDController>("HUD");
        Spawn<IntroCardController>("IntroUI");
        Spawn<PauseMenu>("PauseUI");
        Spawn<ResultScreen>("ResultUI");
        Spawn<SettingsController>("SettingsUI");
        Spawn<DuelToastController>("DuelToastUI");
        Spawn<RivalIndicator>("RivalIndicatorUI");
        Spawn<Minimap>("MinimapUI");
        Spawn<WantedBoard>("WantedUI");
    }

    void OnEnable()
    {
        GameEvents.DuelEnded += OnDuelEnded;
        SceneManager.sceneLoaded += EnforceSingletons;
    }

    void OnDisable()
    {
        GameEvents.DuelEnded -= OnDuelEnded;
        SceneManager.sceneLoaded -= EnforceSingletons;
    }

    static void EnforceSingletons(Scene scene, LoadSceneMode mode)
    {
        var systems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        for (int i = 1; i < systems.Length; i++)
        {
            Destroy(systems[i].gameObject);
        }

        var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        if (listeners.Length > 1)
        {
            var persistent = AudioManager.Instance != null ? AudioManager.Instance.GetComponent<AudioListener>() : null;
            int keep = persistent != null ? Array.IndexOf(listeners, persistent) : -1;
            if (keep < 0)
            {
                keep = Array.FindIndex(listeners, l => l.GetComponent<Camera>() != null);
            }
            if (keep < 0)
            {
                keep = 0;
            }
            for (int i = 0; i < listeners.Length; i++)
            {
                if (i != keep)
                {
                    Destroy(listeners[i]);
                }
            }
        }
    }

    void Start()
    {
        if (State == GameState.Boot)
        {
            SetState(GameState.MainMenu);
        }
    }

    T Spawn<T>(string name) where T : Component
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        return go.AddComponent<T>();
    }

    void SetState(GameState next)
    {
        State = next;
        StateChanged?.Invoke(next);
    }

    public void EnterWorld()
    {
        LastWasCoward = false;
        winStreak = 0;
        StartCoroutine(TransitionTo(DuelScene, GameState.Explore, forceReload: true));
    }

    public void RestartDuel() => EnterWorld();

    public bool BeginEncounter(Goon goon)
    {
        var r = RivalRoster.Current;
        if (!GameSettings.StoryMode && Cowardice.Rolls(r))
        {
            StartCoroutine(CowardFlee(goon, r));
            return false;
        }
        activeGoon = goon;
        Time.timeScale = 1f;
        SetState(GameState.Duel);

        if (GameSettings.StoryMode)
        {
            // Story Mode: Hide "RIVAL 1/5" counter, just show the Goon's Name!
            GameEvents.RaiseRivalIntro(0, 0, goon.DisplayName);
        }
        else
        {
            // Arcade Mode: Get current rival number and total directly from RivalRoster
            int curIndex = r != null ? r.Index + 1 : 1;
            int total = RivalRoster.Count;

            GameEvents.RaiseRivalIntro(curIndex, total, goon.DisplayName);
        }

        return true;
    }

    IEnumerator CowardFlee(Goon goon, Rival r)
    {
        Cowardice.Fleeing = true;
        GameEvents.RaiseRivalFled(r != null ? r.Name : "The rival");

        Wallet.Add(r != null ? Mathf.Max(20, r.Bounty / 5) : 20);
        GameManager.Instance?.AddSteps(4);

        if (goon != null)
        {
            goon.HideUI();
            Vector3 away = FleeDirection(goon.transform.position);
            var runAnim = goon.GetComponentInChildren<SpriteAnimation>();
            float t = 0f;
            while (t < 1.6f)
            {
                t += Time.deltaTime;
                goon.transform.position += away * (12f * Time.deltaTime);
                if (runAnim != null)
                {
                    runAnim.WalkBackward();
                }
                yield return null;
            }
            goon.Defeat();
        }
        Cowardice.Fleeing = false;
    }

    static Vector3 FleeDirection(Vector3 from)
    {
        var pc = FindFirstObjectByType<PlayerController>();
        Vector3 dir = pc != null ? from - pc.transform.position : Vector3.forward;
        dir.y = 0f;
        return dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.forward;
    }

    public string MinigameExitLabel => minigameFromWorld ? "BACK TO TRAIL" : "MENU";

    public void StartMinigame(string key, JobStation job = null)
    {
        if (State == GameState.Explore)
        {
            pendingJob = job;
            StartCoroutine(LaunchMinigameOverworld(key));
        }
        else
        {
            StartCoroutine(LaunchMinigame(key));
        }
    }

    public void EnterMinigame() { Time.timeScale = 1f; SetState(GameState.Minigame); }

    public void DevSetState(GameState s) { Time.timeScale = 1f; SetState(s); }

    public void ToMainMenu()
    {
        if (minigameFromWorld && State == GameState.Minigame)
        {
            StartCoroutine(ReturnToWorld());
            return;
        }

        Time.timeScale = 1f;
        if (string.IsNullOrEmpty(MenuScene))
        {
            SetState(GameState.MainMenu);
        }
        else
        {
            StartCoroutine(TransitionTo(MenuScene, GameState.MainMenu));
        }
    }

    public void TogglePause()
    {
        if (State == GameState.Duel || State == GameState.Explore)
        {
            prePause = State;
            SetPaused(true);
        }
        else if (State == GameState.Paused)
        {
            SetPaused(false);
        }
    }

    void SetPaused(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;
        SetState(paused ? GameState.Paused : prePause);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
{
    Application.Quit();
}
#endif
    }

    IEnumerator TransitionTo(string sceneName, GameState endState, bool startDuel = false, bool forceReload = false)
    {
        var fader = ScreenFader.Instance;
        if (fader != null)
        {
            yield return fader.FadeOut();
        }

        Time.timeScale = 1f;

        bool reload = !string.IsNullOrEmpty(sceneName)
                      && (SceneManager.GetActiveScene().name != sceneName || startDuel || forceReload);
        if (reload && Application.CanStreamedLevelBeLoaded(sceneName))
        {
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone) yield return null;
        }

        SetState(endState);
        if (endState == GameState.Explore)
        {
            RunClock.Instance?.StartRun();
            GameManager.Instance?.ResetTo(startingSteps);
            var r = RivalRoster.Current;
            GameEvents.RaiseRivalIntro(r != null ? r.Index + 1 : 1, RivalRoster.Count, r != null ? r.Name : null);
        }
        if (startDuel)
        {
            GameEvents.RaiseDuelStarted();
            var r = RivalRoster.Current;
            GameEvents.RaiseRivalIntro(r != null ? r.Index + 1 : 1, RivalRoster.Count, r != null ? r.Name : null);
        }

        if (fader != null)
        {
            yield return fader.FadeIn();
        }
    }

    IEnumerator LaunchMinigame(string key)
    {
        var type = MinigameType(key);
        if (type == null)
        {
            yield break;
        }

        var fader = ScreenFader.Instance;
        if (fader != null)
        {
            yield return fader.FadeOut();
        }

        Time.timeScale = 1f;

        var world = SceneManager.GetActiveScene();
        var mg = SceneManager.CreateScene("Minigame_" + key);
        SceneManager.SetActiveScene(mg);

        var host = new GameObject(key);
        SceneManager.MoveGameObjectToScene(host, mg);

        if (world.IsValid() && world.isLoaded && world != mg)
        {
            var op = SceneManager.UnloadSceneAsync(world);
            while (op != null && !op.isDone) yield return null;
        }

        host.AddComponent(type);

        if (fader != null)
        {
            yield return fader.FadeIn();
        }
    }

    IEnumerator LaunchMinigameOverworld(string key)
    {
        var type = MinigameType(key);
        if (type == null)
        {
            yield break;
        }

        var fader = ScreenFader.Instance;
        if (fader != null)
        {
            yield return fader.FadeOut();
        }

        Time.timeScale = 1f;
        minigameFromWorld = true;
        minigameEarnedBefore = Wallet.TotalEarned;
        worldScene = SceneManager.GetActiveScene();

        hiddenRoots.Clear();
        foreach (var root in worldScene.GetRootGameObjects())
        {
            if (!root.activeSelf || root.GetComponentInChildren<EventSystem>(true) != null)
            {
                continue;
            }
            hiddenRoots.Add(root);
            root.SetActive(false);
        }

        var mg = SceneManager.CreateScene("Minigame_" + key);
        SceneManager.SetActiveScene(mg);
        var host = new GameObject(key);
        SceneManager.MoveGameObjectToScene(host, mg);
        host.AddComponent(type);

        if (fader != null)
        {
            yield return fader.FadeIn();
        }
    }

    IEnumerator ReturnToWorld()
    {
        var fader = ScreenFader.Instance;
        if (fader != null)
        {
            yield return fader.FadeOut();
        }

        Time.timeScale = 1f;
        minigameFromWorld = false;

        var mg = SceneManager.GetActiveScene();
        if (worldScene.IsValid() && worldScene.isLoaded)
        {
            SceneManager.SetActiveScene(worldScene);
        }
        if (mg.IsValid() && mg != worldScene)
        {
            var op = SceneManager.UnloadSceneAsync(mg);
            while (op != null && !op.isDone) yield return null;
        }
        foreach (var root in hiddenRoots)
        {
            if (root != null)
            {
                root.SetActive(true);
            }
        }
        hiddenRoots.Clear();

        if (pendingJob != null)
        {
            Destroy(pendingJob.gameObject);
            pendingJob = null;
        }

        int earned = Wallet.TotalEarned - minigameEarnedBefore;
        int reward = Mathf.Clamp(minigameBaseSteps + Mathf.RoundToInt(earned * minigameStepsPerCoin), minigameBaseSteps, minigameMaxSteps);
        GameManager.Instance?.AddSteps(reward);
        GameEvents.RaiseJobResult(reward, earned);
        SetState(GameState.Explore);

        if (fader != null)
        {
            yield return fader.FadeIn();
        }
    }

    static Type MinigameType(string key)
    {
        switch (key)
        {
            case "DynamiteDash": return typeof(DynamiteDash);
            case "TargetRange": return typeof(TargetRange);
            case "BankCrack": return typeof(BankCrack);
            case "Stagecoach": return typeof(Stagecoach);
            default: return null;
        }
    }

    void OnDuelEnded(bool won, float reaction)
    {
        PlayerWon = won;
        LastReaction = reaction;

        var gm = GameManager.Instance;

        if (won)
        {
            winStreak++;
            float mult = 1f + (winStreak - 1) * 0.3f;
            int delta = activeGoon != null ? activeGoon.rewardSteps : 8;
            gm?.AddSteps(delta);
            var r = RivalRoster.Current;
            if (r != null)
            {
                Wallet.Add(Mathf.RoundToInt(r.Bounty * mult));
                RivalRoster.MarkDefeated(r);
            }
            if (activeGoon != null)
            {
                activeGoon.Defeat();
            }
            activeGoon = null;
            GameEvents.RaiseStreakChanged(winStreak);
            GameEvents.RaiseWantedChanged();
            GameEvents.RaiseDuelResolved(true, delta);
            SetState(GameState.Explore);
            return;
        }

        activeGoon = null;
        winStreak = 0;
        GameEvents.RaiseStreakChanged(0);
        if (duelLossIsFatal)
        {
            var duel = DuelController.Instance;
            if (duel != null && duel.LastFalseStart)
            {
                RunClock.Instance?.GameOver("TOO EARLY", "you drew before the call");
            }
            else
            {
                var killer = RivalRoster.Current;
                string gloat = killer != null && !string.IsNullOrEmpty(killer.Gloat) ? $"\"{killer.Gloat}\"" : null;
                RunClock.Instance?.GameOver("YOU DIED", gloat ?? "gunned down in the dust");
            }
            return;
        }

        int penalty = -duelLossPenalty;
        gm?.AddSteps(penalty);
        GameEvents.RaiseDuelResolved(false, penalty);
        if (gm == null || gm.steps > 0)
        {
            SetState(GameState.Explore);
        }
    }

    public int WinStreak => winStreak;
}

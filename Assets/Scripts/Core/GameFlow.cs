using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Boot, MainMenu, Duel, Paused, Result, Minigame }

public class GameFlow : MonoBehaviour
{
    public static GameFlow Instance { get; private set; }

    public const string DuelScene = "OutdoorsScene";
    public const string MenuScene = "";

    public GameState State { get; private set; } = GameState.Boot;
    public event Action<GameState> StateChanged;

    public bool PlayerWon { get; private set; }
    public float LastReaction { get; private set; } = -1f;
    public bool LastWasCoward { get; private set; }

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
    }

    void OnEnable() => GameEvents.DuelEnded += OnDuelEnded;
    void OnDisable() => GameEvents.DuelEnded -= OnDuelEnded;

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

    public void StartDuel()
    {
        var r = RivalRoster.Current;
        if (Cowardice.Rolls(r))
        {
            StartCoroutine(CowardFlee(r));
            return;
        }
        LastWasCoward = false;
        StartCoroutine(TransitionTo(DuelScene, GameState.Duel, true));
    }

    public void RestartDuel()
    {
        LastWasCoward = false;
        StartCoroutine(TransitionTo(DuelScene, GameState.Duel, true));
    }

    IEnumerator CowardFlee(Rival r)
    {
        LastWasCoward = true;
        Cowardice.Fleeing = true;
        GameEvents.RaiseRivalIntro(r.Index + 1, RivalRoster.Count, r.Name);
        yield return new WaitForSecondsRealtime(2.6f);
        Cowardice.Fleeing = false;
        Wallet.Add(Mathf.Max(20, r.Bounty / 5));
        GameEvents.RaiseDuelEnded(true, -1f);
    }

    public void StartMinigame(string scene) => StartCoroutine(TransitionTo(scene, GameState.Minigame));

    public void EnterMinigame() { Time.timeScale = 1f; SetState(GameState.Minigame); }

    public void DevSetState(GameState s) { Time.timeScale = 1f; SetState(s); }

    public void ToMainMenu()
    {
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
        if (State == GameState.Duel)
        {
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
        SetState(paused ? GameState.Paused : GameState.Duel);
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

    IEnumerator TransitionTo(string sceneName, GameState endState, bool startDuel = false)
    {
        var fader = ScreenFader.Instance;
        if (fader != null)
        {
            yield return fader.FadeOut();
        }

        Time.timeScale = 1f;

        bool reload = !string.IsNullOrEmpty(sceneName)
                      && (SceneManager.GetActiveScene().name != sceneName || startDuel);
        if (reload && Application.CanStreamedLevelBeLoaded(sceneName))
        {
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone) yield return null;
        }

        SetState(endState);
        if (startDuel)
        {
            GameEvents.RaiseDuelStarted();
            var r = RivalRoster.Current;
            GameEvents.RaiseRivalIntro(r != null ? r.Index + 1 : 1, RivalRoster.Count, r?.Name);
        }

        if (fader != null)
        {
            yield return fader.FadeIn();
        }
    }

    void OnDuelEnded(bool won, float reaction)
    {
        PlayerWon = won;
        LastReaction = reaction;
        SetState(GameState.Result);
    }
}

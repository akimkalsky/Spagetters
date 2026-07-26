using System.Collections;
using UnityEngine;

public class GameAudio : MonoBehaviour
{
    const string MusicMenu = "menu_theme";
    const string MusicField = "field_theme";
    const string MusicMinigame = "minigame_theme";
    const string MusicPrelude = "duel_prelude_theme";
    const string MusicDuel = "duel_theme";

    int lastStepCount = int.MinValue;
    float lastFootstepTime;

    void Start()
    {
        GameFlow.Instance.StateChanged += OnState;
        AudioManager.Instance?.PlayAmbient("wind");
        OnState(GameFlow.Instance.State);
    }

    void OnEnable()
    {
        GameEvents.StepsChanged += OnSteps;
        GameEvents.Pace += OnPace;
        GameEvents.DrawWindowOpened += OnDraw;
        GameEvents.SteadyShown += OnSteady;
        GameEvents.Shot += OnShot;
        GameEvents.DuelEnded += OnDuelEnded;
        GameEvents.FeintFlashed += OnFeint;
        GameEvents.RivalFled += OnRivalFled;
        GameEvents.StreakChanged += OnStreak;
        GameEvents.FalseStart += OnFalseStart;
    }

    void OnDisable()
    {
        GameEvents.StepsChanged -= OnSteps;
        GameEvents.Pace -= OnPace;
        GameEvents.DrawWindowOpened -= OnDraw;
        GameEvents.SteadyShown -= OnSteady;
        GameEvents.Shot -= OnShot;
        GameEvents.DuelEnded -= OnDuelEnded;
        GameEvents.FeintFlashed -= OnFeint;
        GameEvents.RivalFled -= OnRivalFled;
        GameEvents.StreakChanged -= OnStreak;
        GameEvents.FalseStart -= OnFalseStart;
        if (GameFlow.Instance != null)
        {
            GameFlow.Instance.StateChanged -= OnState;
        }
    }

    void OnState(GameState s)
    {
        var a = AudioManager.Instance;
        if (a == null)
        {
            return;
        }
        bool inField = s == GameState.Duel || s == GameState.Explore;
        a.PlayAmbient("wind", inField ? 0.35f : 0.18f);

        switch (s)
        {
            case GameState.MainMenu:
                a.PlayMusic(MusicMenu, 0.5f);
                break;
            case GameState.Explore:
                a.PlayMusic(MusicField, 0.45f);
                break;
            case GameState.Minigame:
                a.PlayMusic(MusicMinigame, 0.45f);
                break;
            case GameState.Duel:
                a.PlayMusic(MusicPrelude, 0.5f);
                a.PlaySfx("bell", 0.7f);
                break;
        }
    }

    void OnPace(int n)
    {
        if (n > 0)
        {
            AudioManager.Instance?.PlaySfx("tick");
            AudioManager.Instance?.PlaySfx("footstep", 0.5f);
        }
    }

    void OnSteps(int steps)
    {
        bool walked = steps < lastStepCount;
        lastStepCount = steps;
        if (!walked || Time.unscaledTime - lastFootstepTime < 0.3f)
        {
            return;
        }
        lastFootstepTime = Time.unscaledTime;
        AudioManager.Instance?.PlaySfx("footstep", 0.6f);
    }

    void OnSteady()
    {
        var a = AudioManager.Instance;
        if (a == null)
        {
            return;
        }
        a.FadeOutMusic();
        a.PlaySfx("duel_sting", 0.85f);
    }

    void OnDraw()
    {
        AudioManager.Instance?.PlaySfx("draw");
        AudioManager.Instance?.PlaySfx("whoosh", 0.5f);
    }

    void OnShot() => AudioManager.Instance?.PlaySfx("gunshot");

    void OnFeint() => AudioManager.Instance?.PlaySfx("feint", 0.5f);

    void OnRivalFled(string name) => AudioManager.Instance?.PlaySfx("coward", 0.8f);

    void OnFalseStart() => AudioManager.Instance?.PlaySfx("fail", 0.8f);

    void OnStreak(int streak)
    {
        if (streak >= 2)
        {
            AudioManager.Instance?.PlaySfx("streak", 0.7f);
        }
    }

    void OnDuelEnded(bool won, float reaction)
    {
        StartCoroutine(Sting(won));
    }

    IEnumerator Sting(bool won)
    {
        yield return new WaitForSecondsRealtime(0.45f);
        AudioManager.Instance?.PlaySfx(won ? "win" : "lose");
        if (won)
        {
            yield return new WaitForSecondsRealtime(0.2f);
            AudioManager.Instance?.PlaySfx("coin", 0.8f);
        }
    }
}

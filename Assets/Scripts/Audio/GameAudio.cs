using System.Collections;
using UnityEngine;

public class GameAudio : MonoBehaviour
{
    void Start()
    {
        GameFlow.Instance.StateChanged += OnState;
        AudioManager.Instance?.PlayAmbient("wind");
    }

    void OnEnable()
    {
        GameEvents.StepsChanged += OnSteps;
        GameEvents.Pace += OnPace;
        GameEvents.DrawWindowOpened += OnDraw;
        GameEvents.Shot += OnShot;
        GameEvents.DuelEnded += OnDuelEnded;
    }

    void OnDisable()
    {
        GameEvents.StepsChanged -= OnSteps;
        GameEvents.Pace -= OnPace;
        GameEvents.DrawWindowOpened -= OnDraw;
        GameEvents.Shot -= OnShot;
        GameEvents.DuelEnded -= OnDuelEnded;
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
        AudioManager.Instance?.PlaySfx("tick");
        AudioManager.Instance?.PlaySfx("footstep", 0.5f);
    }

    void OnDraw() => AudioManager.Instance?.PlaySfx("draw");

    void OnShot() => AudioManager.Instance?.PlaySfx("gunshot");

    void OnDuelEnded(bool won, float reaction)
    {
        StartCoroutine(Sting(won));
    }

    IEnumerator Sting(bool won)
    {
        yield return new WaitForSecondsRealtime(0.45f);
        AudioManager.Instance?.PlaySfx(won ? "win" : "lose");
    }
}

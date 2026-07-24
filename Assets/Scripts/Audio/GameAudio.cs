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
        GameEvents.DrawWindowOpened += OnDraw;
        GameEvents.DuelEnded += OnDuelEnded;
    }

    void OnDisable()
    {
        GameEvents.StepsChanged -= OnSteps;
        GameEvents.DrawWindowOpened -= OnDraw;
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
        a.PlayAmbient("wind", s == GameState.Duel ? 0.35f : 0.18f);
    }

    void OnSteps(int steps)
    {
        AudioManager.Instance?.PlaySfx("tick");
        AudioManager.Instance?.PlaySfx("footstep", 0.5f);
    }

    void OnDraw() => AudioManager.Instance?.PlaySfx("draw");

    void OnDuelEnded(bool won, float reaction)
    {
        AudioManager.Instance?.PlaySfx("gunshot");
        StartCoroutine(Sting(won));
    }

    IEnumerator Sting(bool won)
    {
        yield return new WaitForSecondsRealtime(0.45f);
        AudioManager.Instance?.PlaySfx(won ? "win" : "lose");
    }
}

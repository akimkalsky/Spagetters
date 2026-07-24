using System;

public static class GameEvents
{
    public static event Action<int> StepsChanged;
    public static event Action OutOfSteps;
    public static event Action DuelStarted;
    public static event Action DrawWindowOpened;
    public static event Action<int, int> RoundChanged;
    public static event Action<int, int, string> RivalIntro;
    public static event Action<bool, float> DuelEnded;

    public static void RaiseStepsChanged(int steps) => StepsChanged?.Invoke(steps);
    public static void RaiseOutOfSteps() => OutOfSteps?.Invoke();
    public static void RaiseDuelStarted() => DuelStarted?.Invoke();
    public static void RaiseDrawWindowOpened() => DrawWindowOpened?.Invoke();
    public static void RaiseRoundChanged(int current, int total) => RoundChanged?.Invoke(current, total);
    public static void RaiseRivalIntro(int current, int total, string name = null) => RivalIntro?.Invoke(current, total, name);
    public static void RaiseDuelEnded(bool playerWon, float reaction = -1f) => DuelEnded?.Invoke(playerWon, reaction);
}

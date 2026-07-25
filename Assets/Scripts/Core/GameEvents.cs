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
    public static event Action<bool, int> DuelResolved;
    public static event Action FeintFlashed;
    public static event Action SteadyShown;
    public static event Action<string> Prompt;
    public static event Action<int, int> JobResult;
    public static event Action<int> Pace;
    public static event Action<string> RivalFled;
    public static event Action Shot;
    public static event Action<int> StreakChanged;
    public static event Action WantedChanged;

    public static void RaiseStepsChanged(int steps) => StepsChanged?.Invoke(steps);
    public static void RaiseOutOfSteps() => OutOfSteps?.Invoke();
    public static void RaiseDuelStarted() => DuelStarted?.Invoke();
    public static void RaiseDrawWindowOpened() => DrawWindowOpened?.Invoke();
    public static void RaiseRoundChanged(int current, int total) => RoundChanged?.Invoke(current, total);
    public static void RaiseRivalIntro(int current, int total, string name = null) => RivalIntro?.Invoke(current, total, name);
    public static void RaiseDuelEnded(bool playerWon, float reaction = -1f) => DuelEnded?.Invoke(playerWon, reaction);
    public static void RaiseDuelResolved(bool playerWon, int stepDelta) => DuelResolved?.Invoke(playerWon, stepDelta);
    public static void RaiseFeintFlashed() => FeintFlashed?.Invoke();
    public static void RaiseSteadyShown() => SteadyShown?.Invoke();
    public static void RaisePrompt(string text) => Prompt?.Invoke(text);
    public static void RaiseJobResult(int steps, int money) => JobResult?.Invoke(steps, money);
    public static void RaisePace(int n) => Pace?.Invoke(n);
    public static void RaiseRivalFled(string name) => RivalFled?.Invoke(name);
    public static void RaiseShot() => Shot?.Invoke();
    public static void RaiseStreakChanged(int streak) => StreakChanged?.Invoke(streak);
    public static void RaiseWantedChanged() => WantedChanged?.Invoke();
}

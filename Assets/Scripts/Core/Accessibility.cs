using System;

public static class Accessibility
{
    public static bool ReduceFlashing { get; private set; }
    public static event Action Changed;

    public static void SetReduceFlashing(bool on)
    {
        ReduceFlashing = on;
        Changed?.Invoke();
    }
}

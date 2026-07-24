using System;

public static class Wallet
{
    public static int Money { get; private set; }
    public static int TotalEarned { get; private set; }
    public static event Action Changed;

    public static void Reset() { Money = 0; TotalEarned = 0; Changed?.Invoke(); }

    public static void Add(int amount)
    {
        if (amount <= 0)
        {
            return;
        }
        Money += amount;
        TotalEarned += amount;
        Changed?.Invoke();
    }

    public static bool TrySpend(int cost)
    {
        if (Money < cost)
        {
            return false;
        }
        Money -= cost;
        Changed?.Invoke();
        return true;
    }
}

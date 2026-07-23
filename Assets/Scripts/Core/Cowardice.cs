using UnityEngine;

public static class Cowardice
{
    public static bool Fleeing;

    public static bool Rolls(Rival r)
    {
        if (r == null || r.Index >= RivalRoster.Count - 2)
        {
            return false;
        }
        return Random.value < 0.18f;
    }
}

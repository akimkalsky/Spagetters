using System.Collections.Generic;
using UnityEngine;

public static class RivalRoster
{
    static Rival[] rivals;
    public static Rival Current { get; private set; }

    public static IReadOnlyList<Rival> All
    {
        get
        {
            if (rivals == null)
            {
                Build();
            }
            return rivals;
        }
    }

    public static int Count => All.Count;

    static void Build()
    {
        var names = new[] { "Clyde Cooldown", "Splitsecond Sam", "Hurried Harry", "Rapid Riley", "Quickfire Quinn" };
        var tagline = new[] { "never missed a mark", "quick and mean", "lets his gun talk", "the house always wins", "digs graves for a living" };
        var taunt = new[] { "You picked the wrong day to die.", "I'll plant you 'fore you blink.", "...", "The odds ain't in your favor, friend.", "I already dug your grave." };
        var gloat = new[] { "Told ya. Never miss.", "Ssso long, partner.", ".", "House always wins.", "Rest easy now." };
        var death = new[] { "How... did you...", "You're faster'n me...", "!", "Bad beat...", "Even I meet my end." };
        var coward = new[] { "...actually, I got somewhere to be.", "On second thought, nope.", "...", "Folded. Cashin' out.", "" };

        rivals = new Rival[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            rivals[i] = new Rival
            {
                Index = i,
                Name = names[i],
                Bounty = 250 * (i + 1) * (i + 1),
                Reaction = Mathf.Lerp(0.45f, 0.18f, (float)i / (names.Length - 1)),
                Seed = 101 + i * 7,
                Defeated = PlayerPrefs.GetInt(Key(i), 0) == 1,
                Tagline = tagline[i],
                Taunt = taunt[i],
                Gloat = gloat[i],
                DeathLine = death[i],
                CowardLine = coward[i],
            };
        }
    }

    static string Key(int i) => "rival_defeated_" + i;

    public static void Select(Rival r) => Current = r;

    public static void MarkDefeated(Rival r)
    {
        if (r == null)
        {
            return;
        }
        r.Defeated = true;
        PlayerPrefs.SetInt(Key(r.Index), 1);
    }

    public static bool AllDefeated()
    {
        foreach (var r in All) if (!r.Defeated)
        {
            return false;
        }
        return true;
    }

    public static int DefeatedCount
    {
        get
        {
            int n = 0;
            foreach (var r in All)
            {
                if (r.Defeated)
                {
                    n++;
                }
            }
            return n;
        }
    }

    public static void ResetAll()
    {
        foreach (var r in All) { r.Defeated = false; PlayerPrefs.SetInt(Key(r.Index), 0); }
        Current = null;
    }

    public static void ResetDefeated()
    {
        foreach (var r in All) { r.Defeated = false; PlayerPrefs.SetInt(Key(r.Index), 0); }
    }

    public static Rival TopUndefeated()
    {
        Rival best = null;
        foreach (var r in All)
        {
            if (!r.Defeated && (best == null || r.Index > best.Index))
            {
                best = r;
            }
        }
        return best;
    }

    public static int Remaining
    {
        get
        {
            int n = 0;
            foreach (var r in All)
            {
                if (!r.Defeated)
                {
                    n++;
                }
            }
            return n;
        }
    }
}

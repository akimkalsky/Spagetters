#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;

public static class DuelBalance
{
    static readonly float[] Skills = { 0.15f, 0.20f, 0.25f, 0.30f, 0.35f, 0.40f };
    static readonly float[] AdvTiers = { 0f, 0.04f, 0.08f, 0.12f };

    [MenuItem("Dev/Duel/Validate Logic")]
    static void Validate()
    {
        int pass = 0, fail = 0;
        var log = new StringBuilder("[DuelBalance] Validation\n");

        void Check(string name, bool ok)
        {
            log.AppendLine($"  {(ok ? "PASS" : "FAIL")}  {name}");
            if (ok) { pass++; } else { fail++; }
        }

        Check("faster player wins", DuelController.ResolveWin(0.20f, 0.30f, true));
        Check("slower player loses", !DuelController.ResolveWin(0.30f, 0.20f, true));
        Check("tie goes to player", DuelController.ResolveWin(0.25f, 0.25f, true));
        Check("strict tie loses", !DuelController.ResolveWin(0.25f, 0.25f, false));

        float easy = DuelController.AiReaction(0.30f, 0.20f);
        float none = DuelController.AiReaction(0.30f, 0f);
        Check("Steady Hand widens margin (adv raises threshold)", easy > none);
        Check("AiReaction floored at 0.05", DuelController.AiReaction(0.30f, -5f) >= 0.05f);

        var all = RivalRoster.All;
        Check("roster has rivals", all.Count >= 2);
        bool descending = true;
        for (int i = 1; i < all.Count; i++)
        {
            if (all[i].Reaction > all[i - 1].Reaction) { descending = false; }
        }
        Check("rival reactions ramp downward (harder)", descending);
        Check("first rival ~0.45s", Mathf.Abs(all[0].Reaction - 0.45f) < 0.01f);
        Check("last rival ~0.18s", Mathf.Abs(all[all.Count - 1].Reaction - 0.18f) < 0.01f);

        var go = new GameObject("~stepEconomyTest");
        try
        {
            var gm = go.AddComponent<GameManager>();
            gm.steps = 5;
            gm.AddSteps(10);
            Check("AddSteps adds", gm.steps == 15);
            gm.AddSteps(-100);
            Check("steps clamp at 0", gm.steps == 0);

            gm.steps = 1;
            bool raised = false;
            System.Action handler = () => raised = true;
            GameEvents.OutOfSteps += handler;
            gm.UseStep();
            GameEvents.OutOfSteps -= handler;
            Check("OutOfSteps fires at 0", raised && gm.steps == 0);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }

        log.AppendLine($"\n{pass} passed, {fail} failed.");
        if (fail == 0) { Debug.Log(log.ToString()); } else { Debug.LogError(log.ToString()); }
    }

    [MenuItem("Dev/Duel/Balance Report")]
    static void Report()
    {
        var all = RivalRoster.All;
        var sb = new StringBuilder("[DuelBalance] Win table  (W = player wins, reaction in seconds)\n");
        sb.AppendLine("Human reaction: ~0.20-0.25s typical, ~0.15s fast.\n");

        foreach (var adv in AdvTiers)
        {
            sb.AppendLine($"── Draw advantage {adv:0.00}s ──");
            sb.Append("rival              thresh   ");
            foreach (var s in Skills) { sb.Append($"{s:0.00} "); }
            sb.AppendLine();

            for (int i = 0; i < all.Count; i++)
            {
                float ai = DuelController.AiReaction(all[i].Reaction, adv);
                sb.Append($"{all[i].Name,-16} {ai,6:0.000}   ");
                foreach (var s in Skills)
                {
                    sb.Append(DuelController.ResolveWin(s, ai, true) ? "  W  " : "  .  ");
                }
                sb.AppendLine();
            }
            sb.AppendLine();
        }
        Debug.Log(sb.ToString());
    }
}
#endif

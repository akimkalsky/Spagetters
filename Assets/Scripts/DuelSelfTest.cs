using System;
using System.Collections;
using UnityEngine;

public class DuelSelfTest : MonoBehaviour
{
    bool ended;
    bool lastWon;

    void OnEnable() => GameEvents.DuelEnded += OnEnded;
    void OnDisable() => GameEvents.DuelEnded -= OnEnded;

    void OnEnded(bool won, float reaction)
    {
        ended = true;
        lastWon = won;
    }

    void Start() => StartCoroutine(Run());

    IEnumerator Run()
    {
        yield return null;
        if (RivalRoster.All.Count > 0)
        {
            RivalRoster.Select(RivalRoster.All[0]);
        }

        var duel = DuelController.EnsureInstance();
        var saved = Snapshot(duel);

        int pass = 0, fail = 0;

        yield return Case(duel, "perfect-draw win", expectWon: true,
            configure: d => d.ForcePerfectDraw = true,
            tally: ok => { if (ok) { pass++; } else { fail++; } });

        yield return Case(duel, "timeout loss", expectWon: false,
            configure: d => { d.ForcePerfectDraw = false; d.timeout = 0.15f; },
            tally: ok => { if (ok) { pass++; } else { fail++; } });

        Restore(duel, saved);
        string msg = $"[DuelSelfTest] {pass} passed, {fail} failed.";
        if (fail == 0) { Debug.Log(msg); } else { Debug.LogError(msg); }
    }

    IEnumerator Case(DuelController duel, string name, bool expectWon, Action<DuelController> configure, Action<bool> tally)
    {
        duel.paceCount = 1;
        duel.paceInterval = 0.02f;
        duel.standoffHold = 0.03f;
        duel.cueDelayMin = 0.02f;
        duel.cueDelayMax = 0.03f;
        duel.timeout = 1.2f;
        duel.playerActor = null;
        duel.rivalActor = null;
        duel.playerAnim = null;
        duel.rivalAnim = null;
        duel.ForcePerfectDraw = false;
        configure(duel);

        ended = false;
        duel.BeginDuel();

        float guard = 0f;
        while (!ended && guard < 10f)
        {
            guard += Time.unscaledDeltaTime;
            yield return null;
        }

        bool ok = ended && lastWon == expectWon;
        Debug.Log($"[DuelSelfTest] {name}: {(ok ? "PASS" : "FAIL")}  (ended={ended}, won={lastWon}, expected={expectWon})");
        tally(ok);
        yield return null;
    }

    static float[] Snapshot(DuelController d)
    {
        return new[] { d.paceCount, d.paceInterval, d.standoffHold, d.cueDelayMin, d.cueDelayMax, d.timeout };
    }

    static void Restore(DuelController d, float[] s)
    {
        d.paceCount = (int)s[0];
        d.paceInterval = s[1];
        d.standoffHold = s[2];
        d.cueDelayMin = s[3];
        d.cueDelayMax = s[4];
        d.timeout = s[5];
        d.ForcePerfectDraw = false;
    }
}

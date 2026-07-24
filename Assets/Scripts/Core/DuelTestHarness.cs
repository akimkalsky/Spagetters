using UnityEngine;

public class DuelTestHarness : MonoBehaviour
{
    public bool forcePerfectDraw;

    string lastOutcome = "-";

    void OnEnable() => GameEvents.DuelEnded += OnDuelEnded;
    void OnDisable() => GameEvents.DuelEnded -= OnDuelEnded;

    void OnDuelEnded(bool won, float reaction)
    {
        string react = reaction >= 0f ? $"{reaction:0.000}s" : "no draw";
        lastOutcome = $"{(won ? "WON" : "LOST")}  ({react})";
    }

    void Update()
    {
        if (DuelController.Instance != null)
        {
            DuelController.Instance.ForcePerfectDraw = forcePerfectDraw;
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(12, 12, 260, 190), GUI.skin.box);
        GUILayout.Label("<b>DEV WORLD</b>", Rich());

        var flow = GameFlow.Instance;
        var gm = GameManager.Instance;
        var duel = DuelController.Instance;
        GUILayout.Label($"State: {(flow != null ? flow.State.ToString() : "-")}");
        GUILayout.Label($"Steps: {(gm != null ? gm.steps : 0)}");
        GUILayout.Label($"Duel: {(duel != null ? duel.Current.ToString() : "-")}");
        GUILayout.Label($"Last: {lastOutcome}");

        GUILayout.Space(6);
        forcePerfectDraw = GUILayout.Toggle(forcePerfectDraw, "Force perfect draw");
        GUILayout.EndArea();
    }

    static GUIStyle _rich;
    static GUIStyle Rich()
    {
        if (_rich == null)
        {
            _rich = new GUIStyle(GUI.skin.label) { richText = true };
        }
        return _rich;
    }
#endif
}

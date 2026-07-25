using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Goon : MonoBehaviour
{
    // Struct nested inside Goon to prevent name collision errors!
    [System.Serializable]
    public struct DialogueLine
    {
        public bool isPlayer; // True = Player, False = Goon
        [TextArea(2, 5)]
        public string text;
    }

    [Header("Gameplay")]
    public int duelDistance = 5;
    public int rewardSteps = 8;
    public int rivalIndex; // which roster rival this NPC is

    [Header("UI")]
    public TMP_Text countdownText;
    public GameObject fightIcon;

    [Header("Story")]
    public bool useRivalRoster = true;
    public string customName = "";
    [TextArea(2, 4)]
    public string customTaunt = ""; // Custom taunt line for the Intro Card!
    public Sprite portrait;

    [Header("Custom Dialogue (Optional)")]
    [Tooltip("Add custom back-and-forth dialogue lines in order.")]
    public List<DialogueLine> customDialogue = new List<DialogueLine>();

    Vector3 fightIconBaseScale = Vector3.one;
    NpcNameTag nameTag;

    private void Start()
    {
        if (fightIcon != null)
        {
            fightIconBaseScale = fightIcon.transform.localScale;
            fightIcon.SetActive(false);
        }

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }

        if (useRivalRoster)
        {
            var r = GetRival();

            if (r != null)
            {
                nameTag = gameObject.AddComponent<NpcNameTag>();
                nameTag.Init(r.Name);
            }
        }
        else if (!string.IsNullOrWhiteSpace(customName))
        {
            nameTag = gameObject.AddComponent<NpcNameTag>();
            nameTag.Init(customName);
        }

        foreach (var col in GetComponentsInChildren<Collider>())
        {
            col.isTrigger = true;
        }

        RefreshBoss();
    }

    void OnEnable() => GameEvents.WantedChanged += RefreshBoss;
    void OnDisable() => GameEvents.WantedChanged -= RefreshBoss;

    void RefreshBoss()
    {
        if (nameTag == null)
        {
            return;
        }
        var boss = RivalRoster.TopUndefeated();
        nameTag.SetBoss(boss != null && boss.Index == rivalIndex);
    }

    public Rival GetRival()
    {
        var all = RivalRoster.All;
        if (all == null || all.Count == 0)
        {
            return RivalRoster.Current;
        }
        return all[Mathf.Clamp(rivalIndex, 0, all.Count - 1)];
    }

    public void UpdateCountdown(int remainingSteps)
    {
        if (remainingSteps <= 0)
        {
            if (countdownText != null) countdownText.gameObject.SetActive(false);
            if (fightIcon != null)
            {
                fightIcon.SetActive(true);
                fightIcon.transform.localScale = fightIconBaseScale * (1f + 0.18f * Mathf.Sin(Time.unscaledTime * 7f));
            }
        }
        else
        {
            if (fightIcon != null) fightIcon.SetActive(false);
            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(true);
                countdownText.text = remainingSteps.ToString();
            }
        }
    }

    public void HideUI()
    {
        if (fightIcon != null) fightIcon.SetActive(false);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
    }

    public void Defeat()
    {
        HideUI();
        gameObject.SetActive(false);
    }

    public string DisplayName
    {
        get
        {
            if (useRivalRoster)
            {
                var rival = GetRival();
                if (rival != null && !string.IsNullOrWhiteSpace(rival.Name))
                    return rival.Name;
            }

            if (!string.IsNullOrWhiteSpace(customName))
                return customName;

            return "GOON";
        }
    }


    public string DisplayTaunt
    {
        get
        {
            // 1. Use Goon's custom taunt if provided
            if (!string.IsNullOrWhiteSpace(customTaunt))
                return customTaunt;

            // 2. Fall back to RivalRoster taunt
            if (useRivalRoster)
            {
                var r = GetRival();
                if (r != null) return Cowardice.Fleeing ? r.CowardLine : r.Taunt;
            }

            return "";
        }
    }

}
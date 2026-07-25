using TMPro;
using UnityEngine;

public class Goon : MonoBehaviour
{
    [Header("Gameplay")]
    public int duelDistance = 5;
    public int rewardSteps = 8;
    public int rivalIndex; // which roster rival this NPC is

    [Header("UI")]
    public TMP_Text countdownText;
    public GameObject fightIcon;



    [Header("Portrait")]
    public Sprite portrait;

    [Header("Dialogue")]
    public string goonName = "Nelson";
    public string[] introDialogue;



    Vector3 fightIconBaseScale = Vector3.one;

    private void Start()
    {
        if (fightIcon != null)
        {
            fightIconBaseScale = fightIcon.transform.localScale;
        }
        fightIcon.SetActive(false);
        countdownText.gameObject.SetActive(false);

        var r = GetRival();
        if (r != null)
        {
            gameObject.AddComponent<NpcNameTag>().Init(r.Name);
        }

        foreach (var col in GetComponentsInChildren<Collider>())
        {
            col.isTrigger = true;
        }
    }

    public Rival GetRival()
    {
        var all = RivalRoster.All;
        if (all.Count == 0)
        {
            return RivalRoster.Current;
        }
        return all[Mathf.Clamp(rivalIndex, 0, all.Count - 1)];
    }

    public void UpdateCountdown(int remainingSteps)
    {
        if (remainingSteps <= 0)
        {
            countdownText.gameObject.SetActive(false);
            fightIcon.SetActive(true);
            fightIcon.transform.localScale = fightIconBaseScale * (1f + 0.18f * Mathf.Sin(Time.unscaledTime * 7f));
        }
        else
        {
            fightIcon.SetActive(false);
            countdownText.gameObject.SetActive(true);
            countdownText.text = remainingSteps.ToString();
        }
    }

    public void HideUI()
    {
        fightIcon.SetActive(false);
        countdownText.gameObject.SetActive(false);
    }

    public void Defeat()
    {
        HideUI();
        gameObject.SetActive(false);
    }
}
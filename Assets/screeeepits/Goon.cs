using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Goon : MonoBehaviour
{
    [Header("Gameplay")]
    public int duelDistance = 5;
    public int rewardSteps = 8;

    [Header("Portrait")]
    public Sprite portrait;

    [Header("Dialogue")]
    public string goonName = "Nelson";
    public string[] introDialogue;

    [Header("World UI")]
    public TMP_Text countdownText;
    public GameObject fightIcon;

    private void Start()
    {
        fightIcon.SetActive(false);
        countdownText.gameObject.SetActive(false);
    }

    public void UpdateCountdown(int remainingSteps)
    {
        if (remainingSteps <= 0)
        {
            countdownText.gameObject.SetActive(false);
            fightIcon.SetActive(true);
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
}
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DuelIntroManager : MonoBehaviour
{
    public static DuelIntroManager Instance;

    [Header("Root")]
    public GameObject duelHolder;

    [Header("Top Panel")]
    public GameObject goonPanel;
    public Image goonPortrait;
    public TMP_Text goonText;

    [Header("Bottom Panel")]
    public GameObject heroPanel;
    public Image playerPortrait;
    public TMP_Text heroText;

    private Goon currentGoon;

    void Awake()
    {
        Instance = this;

        duelHolder.SetActive(false);
        goonPanel.SetActive(false);
        heroPanel.SetActive(false);
    }

    public void PlayIntro(Goon goon)
    {
        currentGoon = goon;

        // Load the goon's portrait
        goonPortrait.sprite = currentGoon.portrait;

        StartCoroutine(IntroRoutine());
    }

    public void EndIntro()
    {
        StopAllCoroutines();

        duelHolder.SetActive(false);

        currentGoon = null;
    }

    IEnumerator IntroRoutine()
    {
        duelHolder.SetActive(true);

        goonPanel.SetActive(true);
        heroPanel.SetActive(true);

        goonText.text = "";
        heroText.text = "";

        yield return new WaitForSeconds(1f);

        // Alternate dialogue:
        // Even lines = Goon
        // Odd lines = Player
        for (int i = 0; i < currentGoon.introDialogue.Length; i++)
        {
            if (i % 2 == 0)
            {
                goonText.text = currentGoon.introDialogue[i];
                heroText.text = "";
            }
            else
            {
                heroText.text = currentGoon.introDialogue[i];
                goonText.text = "";
            }

            yield return new WaitForSeconds(1.5f);
        }

        // Clear dialogue
        goonText.text = "";
        heroText.text = "";

        // Hide the dialogue panels
        goonPanel.SetActive(false);
        heroPanel.SetActive(false);

        yield return new WaitForSeconds(0.5f);

        // Start the actual duel
        DuelController.Instance.BeginDuel();
    }
}
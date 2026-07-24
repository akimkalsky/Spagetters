using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DuelManager : MonoBehaviour
{
    public static DuelManager Instance;

    public enum DuelState
    {
        None,
        Intro,
        Countdown,
        Dodge,
        Aim,
        Resolve,
        End
    }

    public DuelState State { get; private set; }

    [Header("Root")]
    public GameObject duelHolder;

    [Header("Top Panel")]
    public GameObject goonPanel;
    public Image goonPortrait;
    public TMP_Text goonCountdownText;
    public TMP_Text goonText;

    [Header("Bottom Panel")]
    public GameObject heroPanel;
    public Image playerPortrait;

    public TMP_Text heroText;

    [Header("Center")]
    public TMP_Text countdownTimer;
    public TMP_Text heroCountdownText;
    private Goon currentGoon;

    void Awake()
    {
        Instance = this;

        duelHolder.SetActive(false);

        goonCountdownText.gameObject.SetActive(false);
        heroCountdownText.gameObject.SetActive(false);
    }

    public void StartDuel(Goon goon)
    {
        if (State != DuelState.None)
            return;

        currentGoon = goon;

        // Load the goon's portrait
        goonPortrait.sprite = currentGoon.portrait;

        StartCoroutine(DuelRoutine());
    }

    public void EndDuel()
    {
        StopAllCoroutines();

        duelHolder.SetActive(false);

        currentGoon = null;

        State = DuelState.None;
    }

    IEnumerator DuelRoutine()
    {
        duelHolder.SetActive(true);

        State = DuelState.Intro;

        //-------------------------
        // INTRO
        //-------------------------

        State = DuelState.Intro;

        goonPanel.SetActive(true);
        heroPanel.SetActive(true);

        // Clear text
        goonText.text = "";
        heroText.text = "";

        yield return new WaitForSeconds(1.5f);

        // Alternate dialogue
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

        // Clear text
        goonText.text = "";
        heroText.text = "";

        // Hide the dialogue panels
        goonPanel.SetActive(false);
        heroPanel.SetActive(false);

        yield return new WaitForSeconds(0.5f);

  

        //-------------------------
        // COUNTDOWN
        //-------------------------

        State = DuelState.Countdown;

        // Hide dialogue while counting down
        goonText.text = "";
        heroText.text = "";

        goonCountdownText.gameObject.SetActive(true);
        heroCountdownText.gameObject.SetActive(true);

        for (int i = 3; i > 0; i--)
        {
            goonCountdownText.text = i.ToString();
            heroCountdownText.text = i.ToString();

            yield return new WaitForSeconds(1f);
        }

        goonCountdownText.text = "DRAW!";
        heroCountdownText.text = "DRAW!";

        yield return new WaitForSeconds(0.75f);

        // Hide countdown after it's finished
        goonCountdownText.gameObject.SetActive(false);
        heroCountdownText.gameObject.SetActive(false);

        //-------------------------
        // DODGE PHASE
        //-------------------------

        State = DuelState.Dodge;

        Debug.Log("Choose Dodge!");
    }
}
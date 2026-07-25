using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; // Needed for direct input checks

public class DuelIntroUI : MonoBehaviour
{
    public static DuelIntroUI Instance;

    [Header("UI Panels & Text")]
    public GameObject holder;
    public GameObject playerPanel;
    public TMP_Text playerDialogue;
    public GameObject goonPanel;
    public TMP_Text goonDialogue;
    public Image goonPortrait;

    [Header("Settings")]
    public float delayBeforeDuel = 0.5f;

    private DuelController duel;
    private Goon currentGoon;
    private bool isTransitioning = false;
    private bool isActive = false;
    private int dialogueStep = 0;

    void Awake()
    {
        Instance = this;
        if (holder != null) holder.SetActive(false);
    }

    void Update()
    {
        // Only check input while the dialogue is active and not in the ending transition
        if (!isActive || isTransitioning) return;

        // Advance dialogue on Click, Space, or E press
        bool clicked = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                       (Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame));

        if (clicked)
        {
            Next();
        }
    }

    public void Show(Goon goon, DuelController duelController)
    {
        currentGoon = goon;
        duel = duelController;
        isTransitioning = false;
        isActive = true;
        dialogueStep = 0;

        if (goonPortrait != null && goon.portrait != null)
        {
            goonPortrait.sprite = goon.portrait;
        }

        // Freeze player movement/state right away!
        if (GameFlow.Instance != null)
        {
            GameFlow.Instance.BeginEncounter(goon);
        }

        if (holder != null) holder.SetActive(true);

        // Line 1: Player
        ShowPlayerLine("I'm gonna beat you.");
    }

    public void Next()
    {
        if (isTransitioning) return;

        dialogueStep++;

        switch (dialogueStep)
        {
            case 1:
                // Line 2: Enemy response
                ShowGoonLine("We'll see about that.");
                break;

            case 2:
                // Line 3: Player final response
                ShowPlayerLine("Draw!");
                break;

            default:
                // End dialogue sequence
                StartCoroutine(TransitionToDuel());
                break;
        }
    }

    private void ShowPlayerLine(string text)
    {
        playerPanel.SetActive(true);
        goonPanel.SetActive(false);
        playerDialogue.text = text;
    }

    private void ShowGoonLine(string text)
    {
        playerPanel.SetActive(false);
        goonPanel.SetActive(true);
        goonDialogue.text = text;
    }

    private IEnumerator TransitionToDuel()
    {
        isTransitioning = true;
        isActive = false; // Stop listening for inputs

        // Hide speech bubbles
        playerPanel.SetActive(false);
        goonPanel.SetActive(false);

        yield return new WaitForSeconds(delayBeforeDuel);

        if (holder != null) holder.SetActive(false);

        // Start duel sequence
        if (duel != null)
        {
            duel.BeginDuel();
        }
    }
}
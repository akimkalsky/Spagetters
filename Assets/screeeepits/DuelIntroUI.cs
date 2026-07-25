using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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

    [Header("Typewriter Settings")]
    [Tooltip("Time in seconds between each character typed.")]
    public float typingSpeed = 0.03f;
    [Tooltip("Delay in seconds after dialogue finishes before the duel starts.")]
    public float delayBeforeDuel = 0.1f;

    private DuelController duel;
    private Goon currentGoon;
    private bool isTransitioning = false;
    private bool isActive = false;
    private bool isTyping = false;
    private int dialogueIndex = 0;
    private List<Goon.DialogueLine> activeLines;

    private Coroutine typingCoroutine;

    void Awake()
    {
        Instance = this;
        if (holder != null) holder.SetActive(false);
    }

    void Update()
    {
        if (!isActive || isTransitioning) return;

        bool clicked = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                       (Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame));

        if (clicked)
        {
            if (isTyping)
            {
                // If text is still typing, finish typing immediately on click
                CompleteTypingImmediately();
            }
            else
            {
                // Otherwise move to the next line
                Next();
            }
        }
    }

    public void Show(Goon goon, DuelController duelController)
    {
        currentGoon = goon;
        duel = duelController;
        isTransitioning = false;
        isActive = true;
        dialogueIndex = 0;

        if (goonPortrait != null)
        {
            if (goon.portrait != null)
            {
                goonPortrait.sprite = goon.portrait;
                goonPortrait.gameObject.SetActive(true);
            }
            else
            {
                goonPortrait.gameObject.SetActive(false);
            }
        }

        if (GameFlow.Instance != null)
        {
            GameFlow.Instance.BeginEncounter(goon);
        }

        if (goon.customDialogue != null && goon.customDialogue.Count > 0)
        {
            activeLines = goon.customDialogue;
        }
        else
        {
            activeLines = GetDefaultLines();
        }

        if (holder != null) holder.SetActive(true);

        DisplayCurrentLine();
    }

    public void Next()
    {
        if (isTransitioning) return;

        dialogueIndex++;

        if (dialogueIndex < activeLines.Count)
        {
            DisplayCurrentLine();
        }
        else
        {
            StartCoroutine(TransitionToDuel());
        }
    }

    private void DisplayCurrentLine()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        Goon.DialogueLine currentLine = activeLines[dialogueIndex];

        if (currentLine.isPlayer)
        {
            playerPanel.SetActive(true);
            goonPanel.SetActive(false);
            typingCoroutine = StartCoroutine(TypeText(playerDialogue, currentLine.text));
        }
        else
        {
            playerPanel.SetActive(false);
            goonPanel.SetActive(true);
            typingCoroutine = StartCoroutine(TypeText(goonDialogue, currentLine.text));
        }
    }

    private IEnumerator TypeText(TMP_Text targetText, string fullText)
    {
        isTyping = true;
        targetText.text = "";

        foreach (char letter in fullText.ToCharArray())
        {
            targetText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    private void CompleteTypingImmediately()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        Goon.DialogueLine currentLine = activeLines[dialogueIndex];
        if (currentLine.isPlayer)
        {
            playerDialogue.text = currentLine.text;
        }
        else
        {
            goonDialogue.text = currentLine.text;
        }

        isTyping = false;
    }

    private List<Goon.DialogueLine> GetDefaultLines()
    {
        return new List<Goon.DialogueLine>
        {
            new Goon.DialogueLine { isPlayer = true, text = "I'm gonna beat you." },
            new Goon.DialogueLine { isPlayer = false, text = "We'll see about that." },
            new Goon.DialogueLine { isPlayer = true, text = "Draw!" }
        };
    }

    private IEnumerator TransitionToDuel()
    {
        isTransitioning = true;
        isActive = false;

        playerPanel.SetActive(false);
        goonPanel.SetActive(false);

        yield return new WaitForSeconds(delayBeforeDuel);

        if (holder != null) holder.SetActive(false);

        if (duel != null)
        {
            duel.BeginDuel();
        }
    }
}
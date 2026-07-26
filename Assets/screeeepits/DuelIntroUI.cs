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
    public float typingSpeed = 0.05f;
    [Tooltip("Delay in seconds after dialogue finishes before the duel starts.")]
    public float delayBeforeDuel = 0.1f;

    [Header("Advance Controls")]
    public Button nextButton;
    public GameObject readyIndicator;

    [Header("Speaker Labels")]
    public TMP_Text playerName;
    public TMP_Text goonName;

    [Tooltip("Optional decorative frame shown behind the goon portrait.")]
    public GameObject portraitFrame;

    private DuelController duel;
    private Goon currentGoon;
    private bool isTransitioning = false;
    private bool isActive = false;
    private bool isTyping = false;
    private int dialogueIndex = 0;
    private List<Goon.DialogueLine> activeLines;

    private Coroutine typingCoroutine;
    private int lastAdvanceFrame = -1;

    void Awake()
    {
        Instance = this;
        if (holder != null) holder.SetActive(false);
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(Advance);
        }
    }

    void Update()
    {
        if (isActive && !isTransitioning)
        {
            bool clicked = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                           (Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame));

            if (clicked)
            {
                Advance();
            }
        }

        PulseReady();
    }

    public void Advance()
    {
        if (!isActive || isTransitioning)
        {
            return;
        }
        if (Time.frameCount == lastAdvanceFrame)
        {
            return;
        }
        lastAdvanceFrame = Time.frameCount;

        if (isTyping)
        {
            CompleteTypingImmediately();
        }
        else
        {
            Next();
        }
    }

    void SetReady(bool on)
    {
        if (readyIndicator != null && readyIndicator.activeSelf != on)
        {
            readyIndicator.SetActive(on);
        }
    }

    void PulseReady()
    {
        if (readyIndicator == null || !readyIndicator.activeSelf)
        {
            return;
        }
        float s = 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 6f);
        readyIndicator.transform.localScale = new Vector3(s, s, 1f);
    }

    public void Show(Goon goon, DuelController duelController)
    {
        currentGoon = goon;
        duel = duelController;
        isTransitioning = false;
        isActive = true;
        dialogueIndex = 0;

        bool hasPortrait = goon.portrait != null;
        if (goonPortrait != null)
        {
            if (hasPortrait)
            {
                goonPortrait.sprite = goon.portrait;
            }
            goonPortrait.gameObject.SetActive(hasPortrait);
        }
        if (portraitFrame != null)
        {
            portraitFrame.SetActive(hasPortrait);
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

        if (playerName != null) playerName.text = "YOU";
        if (goonName != null) goonName.text = goon.DisplayName;

        SetReady(false);
        if (nextButton != null) nextButton.gameObject.SetActive(true);

        if (holder != null) holder.SetActive(true);
        GameEvents.RaiseDialogueShown(true);

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
        SetReady(false);
        targetText.text = "";

        float baseDelay = Mathf.Max(typingSpeed, 0.05f);
        int typed = 0;
        foreach (char letter in fullText.ToCharArray())
        {
            targetText.text += letter;

            if (letter != ' ' && typed % 2 == 0)
            {
                AudioManager.Instance?.PlaySfx("type", 0.35f);
            }
            typed++;

            float delay = baseDelay;
            if (letter == '.' || letter == '!' || letter == '?')
            {
                delay = baseDelay * 8f;
            }
            else if (letter == ',' || letter == ';' || letter == ':' || letter == '-')
            {
                delay = baseDelay * 4f;
            }
            yield return new WaitForSeconds(delay);
        }

        isTyping = false;
        SetReady(true);
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
        SetReady(true);
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

        SetReady(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);
        playerPanel.SetActive(false);
        goonPanel.SetActive(false);

        yield return new WaitForSeconds(delayBeforeDuel);

        if (holder != null) holder.SetActive(false);
        GameEvents.RaiseDialogueShown(false);

        if (duel != null)
        {
            duel.BeginDuel();
        }
    }
}
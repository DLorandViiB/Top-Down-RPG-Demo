using System;
using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager instance;

    [Header("UI References")]
    public GameObject dialogueBox; // The parent panel
    public TextMeshProUGUI dialogueText;
    public GameObject continueIndicator; // The flashing arrow/icon

    [Header("Typewriter Effect")]
    public float typeSpeed = 0.02f;

    // --- STATE ---
    private Queue<string> sentences;
    private bool isDialogueActive = false;
    private bool isTyping = false;
    private string currentSentence;
    private Action onDialogueCompleteCallback;

    private PlayerMovement playerMovement;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }

        sentences = new Queue<string>();
    }

    void Start()
    {
        // Start hidden
        dialogueBox.SetActive(false);
        playerMovement = FindFirstObjectByType<PlayerMovement>();
    }

    public void StartDialogue(string[] dialogueLines, Action onComplete = null)
    {
        if (isDialogueActive) return;

        Debug.Log("Starting Dialogue...");
        isDialogueActive = true;
        this.onDialogueCompleteCallback = onComplete;

        // Freeze player
        if (playerMovement != null)
        {
            playerMovement.canMove = false;
            playerMovement.StopMovement();
        }

        // Clear old sentences and add the new ones
        sentences.Clear();
        foreach (string sentence in dialogueLines)
        {
            sentences.Enqueue(sentence);
        }

        // Show the box and start
        dialogueBox.SetActive(true);
        DisplayNextSentence();
    }

    public void HandleInput()
    {
        // We only care about input if dialogue is active
        if (!isDialogueActive) return;

        // Check our state
        if (isTyping)
        {
            // Finish the sentence instantly
            StopAllCoroutines();
            dialogueText.text = currentSentence; // Show full text
            isTyping = false;
            continueIndicator.SetActive(true); // Show indicator
        }
        else
        {
            // We're not typing, so advance to next sentence
            DisplayNextSentence();
        }
    }

    public void DisplayNextSentence()
    {
        // We're done typing, hide the indicator
        continueIndicator.SetActive(false);

        // Check if there are any sentences left
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        // Dequeue the next sentence
        currentSentence = sentences.Dequeue();

        // Start the typewriter coroutine
        StopAllCoroutines(); // Stop any previous ones
        StartCoroutine(TypeSentence());
    }

    private IEnumerator TypeSentence()
    {
        isTyping = true;
        dialogueText.text = ""; // Clear text

        foreach (char letter in currentSentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typeSpeed);
        }

        // Finished typing
        isTyping = false;
        continueIndicator.SetActive(true); // Show indicator
        StartCoroutine(BlinkIndicator()); // Start blinking
    }

    private void EndDialogue()
    {
        Debug.Log("Ending Dialogue.");

        // 1. Store the callback in a temp variable
        Action callbackToRun = onDialogueCompleteCallback;

        // 2. Clear the callback and reset state *immediately*
        onDialogueCompleteCallback = null;
        isDialogueActive = false;
        isTyping = false;

        // 3. Stop UI
        StopAllCoroutines();
        dialogueBox.SetActive(false);

        // 4. Run the stored callback (if it exists)
        if (callbackToRun != null)
        {
            callbackToRun.Invoke();
        }

        // 5. Unfreeze the player *only if* a new dialogue wasn't started
        if (!isDialogueActive && playerMovement != null)
        {
            playerMovement.canMove = true;
        }
    }

    private IEnumerator BlinkIndicator()
    {
        // This runs as long as the indicator is active
        while (continueIndicator.activeSelf)
        {
            continueIndicator.transform.localScale = new Vector3(1, 1, 1);
            yield return new WaitForSeconds(0.3f);
            continueIndicator.transform.localScale = new Vector3(1, 0.8f, 1);
            yield return new WaitForSeconds(0.3f);
        }
    }

    // Public getter to check the state
    public bool IsDialogueActive
    {
        get { return isDialogueActive; }
    }
}
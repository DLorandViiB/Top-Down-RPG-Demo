using UnityEngine;

public class SavePoint : MonoBehaviour, IInteractable
{
    public GameObject indicator;

    [TextArea(3, 5)]
    public string saveMessage = "Your progress has been saved.";

    void Start()
    {
        if (indicator) indicator.SetActive(false);
    }

    // This is called by PlayerInteraction.cs when 'Z' is pressed
    public void OnInteract()
    {
        AudioManager.instance.PlaySFX("Heal");

        // 1. Actually save the game
        GameStatemanager.instance.SaveGame();

        // 2. Create the list of sentences to show
        string[] sentences = { saveMessage };

        // 3. Call the DialogueManager to show the message
        DialogueManager.instance.StartDialogue(sentences);
    }

    public void ShowIndicator()
    {
        if (indicator) indicator.SetActive(true);
    }

    public void HideIndicator()
    {
        if (indicator) indicator.SetActive(false);
    }
}
using UnityEngine;
using System.Collections.Generic;

// This script uses the IInteractable interface, just like SavePoint
public class NPC : MonoBehaviour, IInteractable
{
    [Header("UI")]
    public GameObject indicator;

    [Header("State")]
    [Tooltip("A unique ID for this NPC, e.g., 'OldManPete'. This MUST be unique.")]
    public string interactionID;

    [Header("Dialogue")]
    [Tooltip("Dialogue to show BEFORE the item is given.")]
    [TextArea(3, 10)]
    public string[] initialDialogue;

    [Tooltip("Dialogue to show AFTER the item is given.")]
    [TextArea(3, 10)]
    public string[] repeatedDialogue;

    [Header("Items")]
    [Tooltip("Items this NPC will give ONCE upon first interaction.")]
    public List<ItemData> itemsToGive;

    void Start()
    {
        if (indicator) indicator.SetActive(false);
    }

    // This is called by PlayerInteraction.cs when 'Z' is pressed
    public void OnInteract()
    {
        // Check the "Brain" (GameStatemanager) to see if we've
        // already done this interaction.
        if (GameStatemanager.instance.IsInteractionCompleted(interactionID))
        {
            // --- STATE 2: REPEATED INTERACTION ---
            // We've already given the items, so just show the repeated dialogue.
            DialogueManager.instance.StartDialogue(repeatedDialogue, null);
        }
        else
        {
            // --- STATE 1: INITIAL INTERACTION ---
            // This is the first time.
            // We pass the "OnInitialDialogueComplete" function as the callback.
            // This function will run *after* the initial dialogue is finished.
            DialogueManager.instance.StartDialogue(initialDialogue, OnInitialDialogueComplete);
        }
    }

    private void OnInitialDialogueComplete()
    {
        // 1. Mark this interaction as "completed" in the save system
        GameStatemanager.instance.MarkInteractionAsCompleted(interactionID);

        // 2. Check if we have items to give
        if (itemsToGive == null || itemsToGive.Count == 0)
        {
            // No items. We're done.
            return;
        }

        // 3. We have items! Give them and build the "You received" messages.
        List<string> itemMessages = new List<string>();

        Debug.Log($"Giving {itemsToGive.Count} items to player.");
        foreach (ItemData item in itemsToGive)
        {
            InventoryManager.instance.AddItem(item);
            // Create a new message for each item
            itemMessages.Add($"You received {item.itemName}!");
        }

        // 4. Call the DialogueManager *again* to show the item messages.
        DialogueManager.instance.StartDialogue(itemMessages.ToArray(), null);
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
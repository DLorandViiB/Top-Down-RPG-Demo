using UnityEngine;
using System.Collections.Generic; // For the List of items

// This script uses the IInteractable interface, just like NPC and SavePoint
public class TreasureChest : MonoBehaviour, IInteractable
{
    [Header("UI")]
    public GameObject indicator;

    [Header("State")]
    [Tooltip("A unique ID for this chest, e.g., 'ForestChest01'. MUST be unique.")]
    public string interactionID;

    // We'll also need a way to change the sprite
    public Sprite spriteOpened;
    private SpriteRenderer spriteRenderer;

    [Header("Contents")]
    [Tooltip("Items this chest will give ONCE.")]
    public List<ItemData> itemsToGive;

    [Tooltip("Text to show when first opening, e.g., 'You found...'")]
    [TextArea(2, 5)]
    public string openMessage = "You opened the chest...";

    [Tooltip("Text to show if you interact again.")]
    [TextArea(2, 5)]
    public string emptyMessage = "It's empty.";

    private bool isAlreadyOpened = false;

    void Start()
    {
        if (indicator) indicator.SetActive(false);
        spriteRenderer = GetComponent<SpriteRenderer>();

        // --- CHECK ON START ---
        // This is key. When the scene loads, check the save file
        // and set our "opened" state immediately.
        if (GameStatemanager.instance.IsInteractionCompleted(interactionID))
        {
            isAlreadyOpened = true;
            if (spriteRenderer && spriteOpened)
            {
                spriteRenderer.sprite = spriteOpened;
            }
        }
    }

    // This is called by PlayerInteraction.cs when 'Z' is pressed
    public void OnInteract()
    {
        if (isAlreadyOpened)
        {
            // --- STATE 2: ALREADY OPENED ---
            // Just show the "empty" message.
            DialogueManager.instance.StartDialogue(new string[] { emptyMessage });
        }
        else
        {
            // --- STATE 1: FIRST TIME OPENING ---

            // 1. Mark as completed in the save system
            GameStatemanager.instance.MarkInteractionAsCompleted(interactionID);
            isAlreadyOpened = true;

            // 2. Change the sprite
            if (spriteRenderer && spriteOpened)
            {
                spriteRenderer.sprite = spriteOpened;
            }

            // 3. Give the items and build the message list
            List<string> messages = new List<string>();
            messages.Add(openMessage); // Add the "You opened..." intro

            if (itemsToGive != null && itemsToGive.Count > 0)
            {
                foreach (ItemData item in itemsToGive)
                {
                    InventoryManager.instance.AddItem(item);
                    messages.Add($"You received {item.itemName}!");
                }
            }
            else
            {
                messages.Add("...but it was empty.");
            }

            // 4. Show the full message list
            DialogueManager.instance.StartDialogue(messages.ToArray());
        }
    }

    public void ShowIndicator()
    {
        // Don't show the "!" if it's already open
        if (isAlreadyOpened) return;

        if (indicator) indicator.SetActive(true);
    }

    public void HideIndicator()
    {
        if (indicator) indicator.SetActive(false);
    }
}
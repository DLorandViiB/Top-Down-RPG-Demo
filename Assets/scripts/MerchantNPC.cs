using UnityEngine;
using System.Collections.Generic;

// This uses the IInteractable system, just like your other NPCs
public class MerchantNPC : MonoBehaviour, IInteractable
{
    [Header("UI")]
    public GameObject indicator;

    [Header("Shop Stock")]
    [Tooltip("All the items this merchant will sell.")]
    public List<ItemData> itemsForSale;

    [Header("Dialogue")]
    [Tooltip("Dialogue to show when the player interacts.")]
    [TextArea(2, 5)]
    public string greetingMessage = "Welcome! Care to take a look at my wares?";

    [Tooltip("Dialogue to show when the player leaves the shop.")]
    [TextArea(2, 5)]
    public string farewellMessage = "Come back any time!";


    void Start()
    {
        if (indicator) indicator.SetActive(false);
    }

    public void OnInteract()
    {
        // 1. Show the greeting, and when it's done,
        // run the "OpenTheShop" function.
        DialogueManager.instance.StartDialogue(new string[] { greetingMessage }, OpenTheShop);
    }

    private void OpenTheShop()
    {
        // 2. Open the shop UI, passing in our list of items.
        ShopUI.instance.OpenShop(itemsForSale);
    }

    // These are required by IInteractable
    public void ShowIndicator()
    {
        if (indicator) indicator.SetActive(true);
    }

    public void HideIndicator()
    {
        if (indicator) indicator.SetActive(false);
    }
}
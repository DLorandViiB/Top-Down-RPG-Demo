using UnityEngine;
using System.Collections.Generic;

public class BossInteractable : MonoBehaviour, IInteractable
{
    [Header("UI")]
    public GameObject indicator;

    [Header("Boss Config")]
    [Tooltip("A unique ID for this boss, e.g., 'DungeonBoss_01'. MUST be unique.")]
    public string interactionID;

    [Tooltip("The EnemyData asset for this boss.")]
    public EnemyData bossData;

    [Header("Dialogue")]
    [Tooltip("Dialogue to show BEFORE the fight.")]
    [TextArea(3, 10)]
    public string[] preBattleDialogue;

    [Tooltip("Dialogue to show if you interact with the (defeated) boss again.")]
    [TextArea(3, 10)]
    public string postBattleDialogue = "The remains of the fearsome beast lie here.";

    [Header("Boss Visuals")]
    [Tooltip("The GameObject with the boss's sprite, etc. This will be disabled after defeat.")]
    public GameObject bossVisuals;

    private bool isDefeated = false;

    void Start()
    {
        if (indicator) indicator.SetActive(false);

        // --- CHECK SAVE STATE ON START ---
        // Check if this boss is already defeated
        if (GameStatemanager.instance.IsInteractionCompleted(interactionID))
        {
            SetToDefeatedState();
        }
    }

    public void OnInteract()
    {
        if (isDefeated)
        {
            // --- STATE 1: ALREADY DEFEATED ---
            // Just show the "it's dead" message.
            AudioManager.instance.PlaySFX("WorldObjects");
            DialogueManager.instance.StartDialogue(new string[] { postBattleDialogue });
        }
        else
        {
            // --- STATE 2: ALIVE ---
            // Start the pre-battle speech.
            // We pass "OnDialogueComplete" as the callback.
            AudioManager.instance.PlaySFX("WorldObjects");
            DialogueManager.instance.StartDialogue(preBattleDialogue, OnDialogueComplete);
        }
    }

    private void OnDialogueComplete()
    {
        // Tell the GameStatemanager to start the fight
        // AND to remember which boss it is.
        GameStatemanager.instance.SetScriptedBattle(bossData, interactionID);
    }

    private void SetToDefeatedState()
    {
        isDefeated = true;
        if (bossVisuals != null)
        {
            bossVisuals.SetActive(false); // Hide the boss
        }
    }

    public void ShowIndicator()
    {
        if (isDefeated) return; // Don't show "!" if boss is dead
        if (indicator) indicator.SetActive(true);
    }

    public void HideIndicator()
    {
        if (indicator) indicator.SetActive(false);
    }
}
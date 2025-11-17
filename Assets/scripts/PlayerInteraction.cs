using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class PlayerInteraction : MonoBehaviour
{
    private IInteractable targetInteractable;
    private PlayerMovement playerMovement; // For freezing

    void Start()
    {
        // Get our own movement script
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("PlayerInteraction: Could not find PlayerMovement script on this object!");
        }
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (playerMovement == null) return; // Failsafe

        // Check for the 'Z' key press
        if (keyboard.zKey.wasPressedThisFrame)
        {
            // --- Priority 1: Is a dialogue box ALREADY active? ---
            // We *always* want to do this, even if the player is "frozen".
            if (DialogueManager.instance != null && DialogueManager.instance.IsDialogueActive)
            {
                DialogueManager.instance.HandleInput();
            }

            // --- Priority 2: Are we trying to START a new interaction? ---
            // THIS IS THE FIX: We check if the player is "frozen".
            else if (targetInteractable != null && playerMovement.canMove)
            {
                // If both are true, start the interaction.
                targetInteractable.OnInteract();
            }

            // If neither is true (e.g., player is in shop), the "Z" press
            // will be safely ignored by this script.
        }
    }

    // When our trigger collider ENTERS another trigger
    private void OnTriggerEnter2D(Collider2D other)
    {
        IInteractable interactable = other.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            targetInteractable = interactable;
            targetInteractable.ShowIndicator();
        }
    }

    // When our trigger collider EXITS another trigger
    private void OnTriggerExit2D(Collider2D other)
    {
        IInteractable interactable = other.GetComponentInParent<IInteractable>();
        if (interactable != null && interactable == targetInteractable)
        {
            targetInteractable.HideIndicator();
            targetInteractable = null;
        }
    }
}
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))] // Good to have
public class PlayerInteraction : MonoBehaviour
{
    private IInteractable targetInteractable;
    private PlayerMovement playerMovement; // For freezing

    void Start()
    {
        // Get our own movement script
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Check for the 'Z' key press
        if (keyboard.zKey.wasPressedThisFrame)
        {
            // --- Priority 1: Is a dialogue box ALREADY active? ---
            if (DialogueManager.instance != null && DialogueManager.instance.IsDialogueActive)
            {
                DialogueManager.instance.HandleInput();
            }

            // --- Priority 2: Are we trying to START a new interaction? ---
            else if (targetInteractable != null && playerMovement.canMove)
            {
                targetInteractable.OnInteract();
            }
        }
    }

    // When our trigger collider ENTERS another trigger
    private void OnTriggerEnter2D(Collider2D other)
    {
        // We now check for IInteractable on the object *or its parent*.
        IInteractable interactable = other.GetComponentInParent<IInteractable>();

        if (interactable != null)
        {
            // Set it as our current target
            targetInteractable = interactable;

            // Show its indicator (this function name is correct)
            targetInteractable.ShowIndicator();
        }
    }

    // When our trigger collider EXITS another trigger
    private void OnTriggerExit2D(Collider2D other)
    {
        // We also check the parent on exit.
        IInteractable interactable = other.GetComponentInParent<IInteractable>();

        if (interactable != null && interactable == targetInteractable)
        {
            // Hide the indicator
            targetInteractable.HideIndicator();

            // We've walked away, clear our target
            targetInteractable = null;
        }
    }
}
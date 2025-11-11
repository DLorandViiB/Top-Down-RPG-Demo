using UnityEngine;
using UnityEngine.InputSystem;

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
            if (DialogueManager.instance != null && DialogueManager.instance.IsDialogueActive)
            {
                // If dialogue is active, send the "Z" press to the DialogueManager
                DialogueManager.instance.HandleInput();
            }
            else if (targetInteractable != null && playerMovement.canMove)
            {
                // If not, tell our target to do its thing
                targetInteractable.OnInteract();
            }
        }
    }

    // When our trigger collider ENTERS another trigger
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object has an "IInteractable" component
        IInteractable interactable = other.GetComponent<IInteractable>();

        if (interactable != null)
        {
            // Set it as our current target
            targetInteractable = interactable;

            // Show its indicator
            targetInteractable.ShowIndicator();
        }
    }

    // When our trigger collider EXITS another trigger
    private void OnTriggerExit2D(Collider2D other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();

        if (interactable != null && interactable == targetInteractable)
        {
            // Hide the indicator
            targetInteractable.HideIndicator();

            // We've walked away, clear our target
            targetInteractable = null;
        }
    }
}
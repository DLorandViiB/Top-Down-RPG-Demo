using UnityEngine;

[RequireComponent(typeof(Collider2D))] // Needs a trigger collider
public class PlayerInteraction : MonoBehaviour
{
    // This will hold a reference to the interactable object we are currently in range of
    private IInteractable currentInteractable;

    void Update()
    {
        // Check for "Z" key press
        if (Input.GetKeyDown(KeyCode.Z) && currentInteractable != null)
        {
            // We are in range of something and pressed Z! Call its OnInteract function.
            currentInteractable.OnInteract();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // When we enter *any* trigger, check if it has an IInteractable script
        if (other.TryGetComponent<IInteractable>(out IInteractable interactable))
        {
            // It does! Store it and tell it we're in range.
            currentInteractable = interactable;
            currentInteractable.OnPlayerEnterRange();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // When we exit *any* trigger, check if it's the *same one* we are currently interacting with
        if (other.TryGetComponent<IInteractable>(out IInteractable interactable) && interactable == currentInteractable)
        {
            // It is. Tell it we're leaving and clear our reference.
            currentInteractable.OnPlayerExitRange();
            currentInteractable = null;
        }
    }
}
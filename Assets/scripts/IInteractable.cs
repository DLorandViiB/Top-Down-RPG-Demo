public interface IInteractable
{
    // Called by PlayerInteraction when 'Z' is pressed
    void OnInteract();

    // Called by PlayerInteraction when player enters range
    void ShowIndicator();

    // Called by PlayerInteraction when player leaves range
    void HideIndicator();
}
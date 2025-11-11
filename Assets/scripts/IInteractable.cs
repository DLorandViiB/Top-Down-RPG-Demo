public interface IInteractable
{
    /// <summary>
    /// Called when the player presses the interact button ("Z").
    /// </summary>
    void OnInteract();

    /// <summary>
    /// Called by the player when they enter the object's trigger.
    /// Used to show an indicator (e.g., "!")
    /// </summary>
    void OnPlayerEnterRange();

    /// <summary>
    /// Called by the player when they exit the object's trigger.
    /// Used to hide the indicator.
    /// </summary>
    void OnPlayerExitRange();
}
using UnityEngine;

public class SavePoint : MonoBehaviour, IInteractable
{
    [Header("Visuals")]
    [SerializeField]
    private GameObject indicator;

    void Start()
    {
        // Ensure the indicator is hidden at the start
        if (indicator != null)
        {
            indicator.SetActive(false);
        }
    }

    // This function is REQUIRED by the IInteractable contract
    public void OnPlayerEnterRange()
    {
        // Show the "!" indicator
        if (indicator != null)
        {
            indicator.SetActive(true);
        }
    }

    // This function is REQUIRED by the IInteractable contract
    public void OnPlayerExitRange()
    {
        // Hide the "!" indicator
        if (indicator != null)
        {
            indicator.SetActive(false);
        }
    }

    // This function is REQUIRED by the IInteractable contract
    public void OnInteract()
    {
        GameStatemanager.instance.SaveGame();
    }
}
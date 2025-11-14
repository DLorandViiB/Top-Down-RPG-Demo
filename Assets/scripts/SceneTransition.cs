using UnityEngine;
using UnityEngine.SceneManagement;

// This is a simpler version of LockedDoor for unlocked transitions
public class SceneTransition : MonoBehaviour, IInteractable
{
    [Header("UI")]
    public GameObject indicator;

    [Header("Transition")]
    [Tooltip("The *exact* name of the scene to load.")]
    public string sceneToLoad;

    [Tooltip("The spawnPointID in the *next* scene where the player should appear.")]
    public string spawnPointIDInNextScene;

    void Start()
    {
        if (indicator) indicator.SetActive(false);
    }

    // This is called by PlayerInteraction.cs when 'Z' is pressed
    public void OnInteract()
    {
        // 1. Check if the scene and spawn ID are set
        if (string.IsNullOrEmpty(sceneToLoad) || string.IsNullOrEmpty(spawnPointIDInNextScene))
        {
            Debug.LogError("SceneTransition is not configured! Check sceneToLoad and spawnPointID.");
            return;
        }

        // 2. Capture our current state (inventory, stats, etc.)
        GameStatemanager.instance.CaptureCurrentStateForSceneChange();

        // 3. Tell the manager where to spawn us in the *next* scene
        GameStatemanager.instance.SetNextSpawnPoint(spawnPointIDInNextScene);

        // 4. Load the new scene
        SceneManager.LoadScene(sceneToLoad);
    }

    // These functions MUST match your IInteractable contract
    public void ShowIndicator()
    {
        if (indicator) indicator.SetActive(true);
    }

    public void HideIndicator()
    {
        if (indicator) indicator.SetActive(false);
    }
}
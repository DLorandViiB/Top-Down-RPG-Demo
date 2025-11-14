using UnityEngine;
using UnityEngine.SceneManagement;

// We use all our systems: IInteractable, DialogueManager, InventoryManager, GameStatemanager
public class LockedDoor : MonoBehaviour, IInteractable
{
    [Header("UI")]
    public GameObject indicator;

    [Header("State & ID")]
    [Tooltip("A unique ID for this door, e.g., 'DungeonDoor'. This MUST be unique.")]
    public string interactionID;
    private bool isUnlocked = false;

    [Header("Door Config")]
    [Tooltip("The ItemData for the key that unlocks this door.")]
    public ItemData requiredKey;
    [Tooltip("The name of the dungeon scene to load.")]
    public string sceneToLoad;
    [Tooltip("The spawnPointID in the *next* scene where the player should appear.")]
    public string spawnPointIDInNextScene;
    [Tooltip("The collider that makes this door solid. We will disable this.")]
    public Collider2D solidCollider;

    [Header("Visuals")]
    [Tooltip("The sprite to show when the door is unlocked.")]
    public Sprite unlockedSprite;
    private SpriteRenderer spriteRenderer;

    [Header("Dialogue")]
    [Tooltip("Text to show if the player does NOT have the key.")]
    public string lockedMessage = "It's locked. It seems to require a large, ornate key.";
    [Tooltip("Text to show when the player first unlocks the door.")]
    public string unlockMessage = "You used the Dungeon Key. The door creaked open.";

    void Start()
    {
        if (indicator) indicator.SetActive(false);
        spriteRenderer = GetComponent<SpriteRenderer>();

        // --- CHECK SAVE STATE ON START ---
        if (GameStatemanager.instance.IsInteractionCompleted(interactionID))
        {
            SetToUnlockedState();
        }
    }

    public void OnInteract()
    {
        if (isUnlocked)
        {
            // --- STATE 1: ALREADY UNLOCKED ---
            // The door is open, so just load the scene.
            Debug.Log($"Door is already unlocked. Loading scene: {sceneToLoad}");

            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                // We need to tell the GameStatemanager to take a snapshot
                // of our current state BEFORE we load the new scene.
                GameStatemanager.instance.CaptureCurrentStateForSceneChange();

                GameStatemanager.instance.SetNextSpawnPoint(spawnPointIDInNextScene);
                SceneManager.LoadScene(sceneToLoad);
            }
        }
        else
        {
            // --- STATE 2: STILL LOCKED ---
            // Check if the player has the key
            if (InventoryManager.instance.HasItem(requiredKey))
            {
                // --- PLAYER HAS KEY ---
                // 1. Mark as completed in the save system
                GameStatemanager.instance.MarkInteractionAsCompleted(interactionID);

                // 2. Set our internal state
                SetToUnlockedState();

                // 3. Show the "unlocked" message
                DialogueManager.instance.StartDialogue(new string[] { unlockMessage });
            }
            else
            {
                // --- PLAYER DOES NOT HAVE KEY ---
                // 4. Show the "locked" message
                DialogueManager.instance.StartDialogue(new string[] { lockedMessage });
            }
        }
    }

    private void SetToUnlockedState()
    {
        isUnlocked = true;

        // Change the sprite to "open"
        if (spriteRenderer != null && unlockedSprite != null)
        {
            spriteRenderer.sprite = unlockedSprite;
        }

        // Disable the solid collider so the player can pass
        if (solidCollider != null)
        {
            solidCollider.enabled = false;
        }
    }

    public void ShowIndicator()
    {
        if (indicator) indicator.SetActive(true);
    }

    public void HideIndicator()
    {
        if (indicator) indicator.SetActive(false);
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using System.Linq;

public class GameStatemanager : MonoBehaviour
{
    public static GameStatemanager instance; // Singleton

    // --- SAVE SYSTEM ---
    private string saveFilePath;
    private GameData currentGameData;
    private List<string> completedInteractionIDs;

    [Header("Persistent Data")]
    public EnemyData enemyToBattle; // We set this before loading BattleScene

    [Header("Transition UI")]
    private Image fadeScreen;
    private TextMeshProUGUI encounterText;
    public float fadeSpeed = 1.5f;

    private PlayerMovement playerMovement; // To freeze the player
    private Rigidbody2D playerRb;

    [Header("Encounter Cooldown")]
    public float encounterCooldownTime = 5.0f;
    public bool isEncounterOnCooldown = false;

    private SpriteRenderer playerSpriteRenderer;
    public string overworldSceneName = "MainWorldScene";

    private bool isLoadingFromBattle = false;

    void Awake()
    {
        // This is the "boss" singleton.
        // It's the only one that calls DontDestroyOnLoad.
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject); // It persists itself

            // Initialize the save system path
            saveFilePath = Path.Combine(Application.persistentDataPath, "gamedata.json");

            // Subscribe to the scene loaded event
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            // If another one tries to create itself, destroy it.
            Destroy(this.gameObject);
        }
    }

    #region Scene Management

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene Loaded: {scene.name}");

        // --- 1. FIND UI REFS ---
        SceneUIRefs uiRefs = FindFirstObjectByType<SceneUIRefs>();
        if (uiRefs != null)
        {
            this.fadeScreen = uiRefs.fadeScreen;
            this.encounterText = uiRefs.encounterText;
            Debug.Log("GameStatemanager: Successfully linked UI references.");
        }
        else
        {
            Debug.LogWarning("GameStatemanager: Could not find SceneUIRefs object in this scene!");
        }

        // --- 2. HANDLE MAIN WORLD ---
        if (scene.name == overworldSceneName)
        {
            // The scene is loaded, NOW we can safely find the player
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerMovement = playerObj.GetComponent<PlayerMovement>();
                playerSpriteRenderer = playerObj.GetComponentInChildren<SpriteRenderer>();

                // Re-link the camera
                CameraFollow camFollow = FindFirstObjectByType<CameraFollow>();
                if (camFollow != null)
                {
                    camFollow.target = playerObj.transform;
                }
                else
                {
                    Debug.LogWarning("GameStatemanager: Could not find CameraFollow script!");
                }
            }
            else
            {
                Debug.LogError("GameStatemanager: Could not find object with tag 'Player' in scene!");
            }

            if (this.isLoadingFromBattle)
            {
                // --- 1. WE ARE RETURNING FROM A BATTLE ---
                if (playerObj != null && this.currentGameData != null)
                {
                    // Stop physics and restore position
                    Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
                    if (rb) rb.linearVelocity = Vector2.zero;
                    playerObj.transform.position = this.currentGameData.playerPosition;

                    Debug.Log("Returned from battle. Restored position to: " + this.currentGameData.playerPosition);
                }

                PlayerStats.instance.NotifyStatsChanged();
                InventoryManager.instance.NotifyInventoryChanged();

                // Reset the flag for next time.
                this.isLoadingFromBattle = false;
            }
            else
            {
                // --- 2. WE ARE LOADING FROM THE MENU (NEW or CONTINUE) ---
                if (this.currentGameData != null)
                {
                    // This is a "Continue Game". currentGameData was loaded from a file.
                    // We MUST apply the data.
                    Debug.Log("Continuing game. Applying loaded data.");
                    ApplyGameData(this.currentGameData, playerObj);
                }
                else
                {
                    // This is a "New Game". currentGameData is null.
                    Debug.Log("Starting a new game. Using default manager states.");
                    InventoryManager.instance.AddStartingItems();
                    this.completedInteractionIDs = new List<string>();
                }
            }
        }

        // --- 3. FADE IN ---
        StartCoroutine(FadeIn());
    }

    public void StartNewGame()
    {
        // We're starting fresh, so create a new, default GameData object
        this.currentGameData = null;
        // The OnSceneLoaded event will handle applying this default data
    }

    public void ContinueGame()
    {
        // Try to load the game from the file
        // If it fails, just start a new game
        if (!LoadGame())
        {
            StartNewGame();
        }
        // The OnSceneLoaded event will handle applying the loaded data
    }

    #endregion

    #region Save/Load System

    public bool DoesSaveFileExist()
    {
        return File.Exists(saveFilePath);
    }

    public void SaveGame()
    {
        if (PlayerStats.instance == null || InventoryManager.instance == null)
        {
            Debug.LogError("Cannot save! Player systems not found.");
            return;
        }

        Debug.Log("Saving game to: " + saveFilePath);

        // 1. GATHER DATA
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("Cannot save! Player object not found in scene.");
            return;
        }
        GameData data = GatherGameData(playerObj);

        // 2. SERIALIZE & SAVE
        string json = JsonUtility.ToJson(data, true); // 'true' for pretty-printing
        File.WriteAllText(saveFilePath, json);

        this.currentGameData = data;

        Debug.Log("Game saved successfully.");
        // We can also call our UI to show a message
        // Example: UIManager.Instance.ShowBottomMessage("Game has been saved!", 3f);
    }

    public bool LoadGame()
    {
        if (!DoesSaveFileExist())
        {
            Debug.LogError("No save file found to load.");
            return false;
        }

        Debug.Log("Loading game from: " + saveFilePath);

        // 1. LOAD & DESERIALIZE
        string json = File.ReadAllText(saveFilePath);
        GameData data = JsonUtility.FromJson<GameData>(json);

        // 2. STORE DATA
        // We store the data here.
        // OnSceneLoaded will be responsible for APPLYING it.
        this.currentGameData = data;

        Debug.Log("Game data loaded.");
        return true;
    }

    private GameData GatherGameData(GameObject playerObj)
    {
        GameData data = new GameData();
        PlayerStats stats = PlayerStats.instance;
        InventoryManager inventory = InventoryManager.instance;

        // 1. Get Player Position
        if (playerObj != null)
        {
            data.playerPosition = playerObj.transform.position;
        }

        // 2. Get Player Stats
        data.level = stats.level;
        data.maxHealth = stats.maxHealth;
        data.currentHealth = stats.currentHealth;
        data.maxMana = stats.maxMana;
        data.currentMana = stats.currentMana;
        data.attack = stats.attack;
        data.defense = stats.defense;
        data.luck = stats.luck;
        data.skillPoints = stats.skillPoints;
        data.currentXP = stats.currentXP;
        data.xpToNextLevel = stats.xpToNextLevel;

        // 3. Get Inventory
        data.inventoryItemIDs = new List<string>();
        data.inventoryItemQuantities = new List<int>();
        foreach (InventorySlot slot in inventory.slots)
        {
            if (slot.item != null)
            {
                data.inventoryItemIDs.Add(slot.item.itemName);
                data.inventoryItemQuantities.Add(slot.quantity);
            }
            else
            {
                data.inventoryItemIDs.Add(null);
                data.inventoryItemQuantities.Add(0);
            }
        }

        // 4. Get Skills
        data.unlockedSkillIDs = new List<string>();
        foreach (SkillData skill in stats.unlockedSkills)
        {
            data.unlockedSkillIDs.Add(skill.skillName);
        }

        // 5. Get World State
        data.completedInteractionIDs = this.completedInteractionIDs;

        return data;
    }

    private void ApplyGameData(GameData data, GameObject playerObj)
    {
        PlayerStats stats = PlayerStats.instance;
        InventoryManager inventory = InventoryManager.instance;
        AssetManager assets = AssetManager.instance;

        Debug.Log("Applying game data to systems...");

        // 1. Apply Player Position
        if (playerObj != null)
        {
            playerObj.transform.position = data.playerPosition;
        }

        // 2. Apply Player Stats
        stats.level = data.level;
        stats.maxHealth = data.maxHealth;
        stats.currentHealth = data.currentHealth;
        stats.maxMana = data.maxMana;
        stats.currentMana = data.currentMana;
        stats.attack = data.attack;
        stats.defense = data.defense;
        stats.luck = data.luck;
        stats.skillPoints = data.skillPoints;
        stats.currentXP = data.currentXP;
        stats.xpToNextLevel = data.xpToNextLevel;

        // 3. Apply Skills (This just repopulates the skill list)
        stats.unlockedSkills.Clear();
        foreach (string skillName in data.unlockedSkillIDs)
        {
            SkillData skill = assets.GetSkillByName(skillName);
            if (skill != null)
            {
                stats.unlockedSkills.Add(skill);
            }
        }

        // 4. Apply Inventory

        // Loop through the slots in the InventoryManager (which has 24)
        for (int i = 0; i < inventory.slots.Count; i++)
        {
            // Check if the loaded data has an item for this slot
            if (i < data.inventoryItemIDs.Count)
            {
                string itemName = data.inventoryItemIDs[i];
                if (!string.IsNullOrEmpty(itemName))
                {
                    // Load the item from the save file
                    inventory.slots[i].item = assets.GetItemByName(itemName);
                    inventory.slots[i].quantity = data.inventoryItemQuantities[i];
                }
                else
                {
                    // This slot was empty in the save file
                    inventory.slots[i].item = null;
                    inventory.slots[i].quantity = 0;
                }
            }
            else
            {
                // This slot is beyond what the save file had (e.g., a new game).
                // It should just be empty.
                inventory.slots[i].item = null;
                inventory.slots[i].quantity = 0;
            }
        }

        // 5. Apply World State
        this.completedInteractionIDs = data.completedInteractionIDs;

        // Manually trigger the UI update events
        stats.NotifyStatsChanged();
        inventory.NotifyInventoryChanged();

        Debug.Log("Game data applied successfully.");
    }

    #endregion

    // --- BATTLE & TRANSITION LOGIC (Your existing code) ---

    #region Battle & Transition Logic

    public void StartBattle(EnemyData enemy)
    {
        // playerMovement is now found by OnSceneLoaded, so we just check if it exists
        if (playerMovement == null)
        {
            Debug.LogError("GameStatemanager: playerMovement is null. Cannot start battle.");
            return;
        }

        StartCoroutine(BattleTransition(enemy));
    }

    private IEnumerator BattleTransition(EnemyData enemy)
    {
        // 0. SNAPSHOT THE CURRENT GAME STATE
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            this.currentGameData = GatherGameData(playerObj);
            Debug.Log("Game state captured before battle at: " + this.currentGameData.playerPosition);
        }
        else
        {
            Debug.LogError("Could not find Player to capture state! Aborting battle.");
            yield break; // Stop the coroutine
        }

        // 1. Store enemy and freeze player
        enemyToBattle = enemy;
        playerMovement.canMove = false;
        playerMovement.StopMovement();

        // 2. Show "Enemy approaching!" text
        if (encounterText) encounterText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        if (encounterText) encounterText.gameObject.SetActive(false);

        // 3. Fade to black
        yield return StartCoroutine(FadeOut());

        // 4. Load the battle scene
        yield return SceneManager.LoadSceneAsync("BattleScene");
        }

        public void EndBattle()
        {
            StartCoroutine(EndBattleTransition());
        }

    private IEnumerator EndBattleTransition()
    {
        // 1. Fade to black
        yield return StartCoroutine(FadeOut());

        // 2. Load the main world
        this.isLoadingFromBattle = true;
        yield return SceneManager.LoadSceneAsync(overworldSceneName);


        // 3. Clear buffs
        PlayerStats.instance.ClearAllBuffs();

        // 4. Re-enable all encounter zones
        EncounterZone[] zones = FindObjectsByType<EncounterZone>(FindObjectsSortMode.None);
        foreach (EncounterZone zone in zones)
        {
            zone.enabled = true;
        }

        // 5. Start the "safe" cooldown
        StartCoroutine(EncounterCooldown());

        // 6. Fade back in (this happens in the new scene)
        yield return StartCoroutine(FadeIn());

        // 7. AFTER fade-in, unfreeze the player.
        if (playerMovement != null) playerMovement.canMove = true;
    }

    #endregion

    /// <summary>
    /// Checks if a specific one-time event (like an NPC giving an item)
    /// has already been completed.
    /// </summary>
    public bool IsInteractionCompleted(string id)
    {
        if (this.completedInteractionIDs == null)
        {
            this.completedInteractionIDs = new List<string>();
        }
        return this.completedInteractionIDs.Contains(id);
    }

    /// <summary>
    /// Marks a one-time event as completed and saves it to memory.
    /// (It will be saved to the file on the next SaveGame() call)
    /// </summary>
    public void MarkInteractionAsCompleted(string id)
    {
        if (this.completedInteractionIDs == null)
        {
            this.completedInteractionIDs = new List<string>();
        }

        if (!this.completedInteractionIDs.Contains(id))
        {
            this.completedInteractionIDs.Add(id);
        }
    }

    private IEnumerator EncounterCooldown()
    {
        isEncounterOnCooldown = true;
        yield return new WaitForSeconds(encounterCooldownTime);
        isEncounterOnCooldown = false;
    }

    public IEnumerator FadeOut()
    {
        if (!fadeScreen) { Debug.LogWarning("No fade screen assigned."); yield break; }
        fadeScreen.color = new Color(0, 0, 0, 0);
        float alpha = 0;
        while (alpha < 1)
        {
            alpha += Time.deltaTime * fadeSpeed;
            fadeScreen.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeScreen.color = new Color(0, 0, 0, 1);
    }

    public IEnumerator FadeIn()
    {
        if (!fadeScreen) { Debug.LogWarning("No fade screen assigned."); yield break; }
        fadeScreen.color = new Color(0, 0, 0, 1);
        float alpha = 1;
        while (alpha > 0)
        {
            alpha -= Time.deltaTime * fadeSpeed;
            fadeScreen.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeScreen.color = new Color(0, 0, 0, 0);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
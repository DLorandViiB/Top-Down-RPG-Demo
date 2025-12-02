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
    private string nextSpawnPointID;

    private string currentScriptedBattleID;
    private string sceneToReturnTo;
    private int uiPauseCounter = 0;

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

    public void SetNextSpawnPoint(string spawnID)
    {
        this.nextSpawnPointID = spawnID;
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

        // --- 2. HANDLE PLAYER AND CAMERA (THE FIX) ---
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            // We found a player! This is a gameplay scene.
            // Set up the playerMovement variable.
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

            // --- 3. HANDLE DATA & SPAWNING ---
            if (this.isLoadingFromBattle)
            {
                // --- 3a. WE ARE RETURNING FROM A BATTLE ---
                if (this.currentGameData != null)
                {
                    Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
                    if (rb) rb.linearVelocity = Vector2.zero;
                    playerObj.transform.position = this.currentGameData.playerPosition;
                    Debug.Log("Returned from battle. Restored position to: " + this.currentGameData.playerPosition);
                }

                // Manually trigger UI updates for stats/items gained in battle
                PlayerStats.instance.NotifyStatsChanged();
                InventoryManager.instance.NotifyInventoryChanged();
                this.isLoadingFromBattle = false;
            }
            else
            {
                // --- 3b. WE ARE LOADING FROM THE MENU (NEW or CONTINUE) ---
                if (this.currentGameData != null)
                {
                    // This is a "Continue Game". Apply the loaded data.
                    Debug.Log("Continuing game. Applying loaded data.");
                    ApplyGameData(this.currentGameData, playerObj);
                }
                else
                {
                    // This is a "New Game". Use default states.
                    Debug.Log("Starting a new game. Using default manager states.");
                    InventoryManager.instance.AddStartingItems();
                    this.completedInteractionIDs = new List<string>();
                }
            }
        }

        // --- 4. FADE IN ---
        StartCoroutine(FadeIn());

        // --- 5. CHECK FOR A FORCED SPAWN POINT ---
        if (!string.IsNullOrEmpty(this.nextSpawnPointID))
        {
            PlayerSpawnPoint[] spawnPoints = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);
            foreach (PlayerSpawnPoint spawnPoint in spawnPoints)
            {
                if (spawnPoint.spawnPointID == this.nextSpawnPointID)
                {
                    // Found our spawn point!
                    if (playerObj != null)
                    {
                        // Stop physics and teleport
                        Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
                        if (rb) rb.linearVelocity = Vector2.zero;

                        playerObj.transform.position = spawnPoint.transform.position;
                        Debug.Log($"Player spawned at: {this.nextSpawnPointID}");
                    }
                    break; // Stop looping
                }
            }

            // We've used the ID, so clear it for next time.
            this.nextSpawnPointID = null;
        }

        // MUSIC LOGIC
        if (scene.name == "MainWorldScene")
        {
            AudioManager.instance.PlayMusic("MainTheme");
        }
        else if (scene.name == "DungeonScene")
        {
            AudioManager.instance.PlayMusic("DungeonTheme"); // If you have one, or use MainTheme
        }
        else if (scene.name == "BattleScene")
        {
            AudioManager.instance.PlayMusic("BattleTheme");
        }
    }

    public void CaptureCurrentStateForSceneChange()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            this.currentGameData = GatherGameData(playerObj);
            Debug.Log("Game state captured for scene change at: " + this.currentGameData.playerPosition);
        }
        else
        {
            Debug.LogWarning("Could not find Player to capture state! State was not saved.");
        }
    }

    public void StartNewGame()
    {
        // We're starting fresh, so set data to null.
        // OnSceneLoaded will see this and trigger a "New Game" setup.
        this.currentGameData = null;

        SceneManager.LoadScene(overworldSceneName);
    }

    public void ContinueGame()
    {
        // 1. Try to load the game from the file
        if (!LoadGame()) // LoadGame() loads data into this.currentGameData
        {
            Debug.LogWarning("Continue failed. Starting New Game.");
            StartNewGame();
            return;
        }

        // 2. We have data! Load the correct scene.
        SceneManager.LoadScene(this.currentGameData.sceneName);
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
        data.currentCurrency = stats.currentCurrency;

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

        data.sceneName = SceneManager.GetActiveScene().name;

        // --- SAVE QUESTS ---
        data.activeQuestIDs = new List<string>();
        data.activeQuestProgress = new List<int>();

        foreach (Quest q in QuestManager.instance.activeQuests)
        {
            data.activeQuestIDs.Add(q.data.questID);
            data.activeQuestProgress.Add(q.currentAmount);
        }

        data.completedQuestIDs = QuestManager.instance.completedQuestIDs;

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
        stats.currentCurrency = data.currentCurrency;

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

        // --- LOAD QUESTS ---
        QuestManager qManager = QuestManager.instance;
        qManager.activeQuests.Clear();
        qManager.completedQuestIDs.Clear();

        // Restore Completed
        qManager.completedQuestIDs = data.completedQuestIDs;

        // Restore Active
        for (int i = 0; i < data.activeQuestIDs.Count; i++)
        {
            string id = data.activeQuestIDs[i];
            int progress = data.activeQuestProgress[i];

            // Use the lookup to get the data back
            QuestData qData = qManager.GetQuestDataByID(id);
            if (qData != null)
            {
                Quest newQuest = new Quest(qData);
                newQuest.currentAmount = progress;
                qManager.activeQuests.Add(newQuest);
            }
        }

        // Manually trigger the UI update events
        stats.NotifyStatsChanged();
        inventory.NotifyInventoryChanged();

        Debug.Log("Game data applied successfully.");
    }

    /// <summary>
    /// Called by a UI script (like ShopUI) to request a game pause.
    /// </summary>
    public void RequestUIPause()
    {
        // "Lazy find" the player if we don't have it
        if (playerMovement == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj)
                playerMovement = playerObj.GetComponent<PlayerMovement>();
        }

        uiPauseCounter++; // Add this menu to the "pause list"

        // If this is the *first* menu to open, pause the game.
        if (uiPauseCounter == 1)
        {
            Time.timeScale = 0f;
            if (playerMovement != null)
            {
                playerMovement.canMove = false;
                playerMovement.StopMovement();
            }
        }
    }

    /// <summary>
    /// Called by a UI script when it closes.
    /// </summary>
    public void ReleaseUIPause()
    {
        uiPauseCounter--; // Remove this menu from the "pause list"

        // If this was the *last* menu to close, un-pause the game.
        if (uiPauseCounter <= 0)
        {
            uiPauseCounter = 0; // Failsafe
            Time.timeScale = 1f;
            if (playerMovement != null)
            {
                playerMovement.canMove = true;
            }
        }
    }

    #endregion

    public void SetScriptedBattle(EnemyData enemy, string interactionID)
    {
        this.currentScriptedBattleID = interactionID;
        StartBattle(enemy);
    }

    // --- BATTLE & TRANSITION LOGIC (Your existing code) ---

    #region Battle & Transition Logic

    public void StartBattle(EnemyData enemy)
    {
        this.sceneToReturnTo = SceneManager.GetActiveScene().name;

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
        AudioManager.instance.PlaySFX("EnemyEncounter");

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
            AudioManager.instance.PlaySFX("EnemyDefeated");
            StartCoroutine(EndBattleTransition());
        }

    private IEnumerator EndBattleTransition()
    {
        if (enemyToBattle.isBoss && !string.IsNullOrEmpty(currentScriptedBattleID))
        {
            // We did! Mark this boss as "completed" in the save file.
            MarkInteractionAsCompleted(currentScriptedBattleID);
            Debug.Log($"Boss {currentScriptedBattleID} defeated and marked as complete.");

            // Clear the ID so we don't re-mark it.
            currentScriptedBattleID = null;
        }

        // 1. Fade to black
        yield return StartCoroutine(FadeOut());

        // 2. Load back from the battle
        this.isLoadingFromBattle = true;
        string sceneName = string.IsNullOrEmpty(this.sceneToReturnTo) ? overworldSceneName : this.sceneToReturnTo;

        yield return SceneManager.LoadSceneAsync(sceneName);


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

    public void TriggerGameOver()
    {
        // We just start the coroutine.
        StartCoroutine(GameOverCoroutine());
    }

    private IEnumerator GameOverCoroutine()
    {
        // Wait 1 second (to let the player read the "defeated" message)
        yield return new WaitForSeconds(1.0f);

        // Fade to black
        yield return StartCoroutine(FadeOut());

        // NOW, we safely load the scene from the persistent manager
        SceneManager.LoadScene("DeathScene");
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
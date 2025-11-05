using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    private enum BattleMenuState { ActionSelect, SkillSelect }
    private BattleMenuState currentState;

    [Header("Player")]
    private PlayerStats playerStats;

    [Header("Enemy")]
    private EnemyBattleController enemy;

    [Header("UI References")]
    public Slider playerHealthBar;
    public TextMeshProUGUI playerHPText;
    public Slider playerManaBar;
    public TextMeshProUGUI playerMPText;

    public Slider enemyHealthBar;
    //public TextMeshProUGUI enemyNameText;
    public TextMeshProUGUI commentText;

    [Header("Action Buttons")]
    public Button[] actionButtons;
    public GameObject actionOptionsPanel;
    private int currentActionIndex = 0;
    private bool isPlayerTurn = true;

    [Header("Skill Panel")]
    public GameObject skillListPanel;
    public GameObject skillButtonPrefab;
    private int currentSkillIndex = 0;
    public Transform skillRow1;
    public Transform skillRow2;

    private List<SkillButton> currentSkillButtons = new List<SkillButton>();

    void Start()
    {
        playerStats = PlayerStats.instance;
        enemy = FindFirstObjectByType<EnemyBattleController>();

        if (playerStats != null)
        {
            SpriteRenderer playerSprite = playerStats.GetComponentInChildren<SpriteRenderer>();
            if (playerSprite != null)
            {
                playerSprite.enabled = false;
            }
        }

        if (GameStatemanager.instance != null && GameStatemanager.instance.enemyToBattle != null)
        {
            enemy.Setup(GameStatemanager.instance.enemyToBattle);
        }
        else
        {
            enemy.Setup(null);
        }

        UpdatePlayerUI();
        UpdateEnemyUI();

        if (GameStatemanager.instance != null)
        {
            StartCoroutine(GameStatemanager.instance.FadeIn());
        }

        actionButtons[0].onClick.AddListener(OnFightButton);
        actionButtons[1].onClick.AddListener(OnSkillButton);
        actionButtons[2].onClick.AddListener(OnItemButton);
        actionButtons[3].onClick.AddListener(OnRunButton);

        currentState = BattleMenuState.ActionSelect;
        skillListPanel.SetActive(false); // Skills are hidden
        actionButtons[0].Select();
    }

    public void UpdatePlayerUI()
    {
        if (playerStats == null)
        {
            Debug.LogError("BattleManager can't find PlayerStats!");
            return;
        }

        playerHealthBar.maxValue = playerStats.maxHealth;

        playerHealthBar.value = playerStats.currentHealth;

        playerHPText.SetText($"HP: {playerStats.currentHealth} / {playerStats.maxHealth}");

        playerManaBar.maxValue = playerStats.maxMana;
        playerManaBar.value = playerStats.currentMana;
        playerMPText.SetText($"MP: {playerStats.currentMana} / {playerStats.maxMana}");
    }

    public void UpdateEnemyUI()
    {
        if (enemy == null || enemy.enemyData == null) return;

        enemyHealthBar.maxValue = enemy.enemyData.maxHealth;
        enemyHealthBar.value = enemy.currentHealth;
        //enemyNameText.SetText(enemy.enemyData.enemyName);
    }

    void Update()
    {
        // If it's not our turn, don't allow any menu input
        if (!isPlayerTurn) return;

        // --- 'X' KEY (CANCEL) LOGIC ---
        if (currentState == BattleMenuState.SkillSelect && Input.GetKeyDown(KeyCode.X))
        {
            GoBackToActions();
            return; // Don't process any other input this frame
        }

        // Check which menu we're currently in
        if (currentState == BattleMenuState.ActionSelect)
        {
            HandleActionNavigation();
        }
        else if (currentState == BattleMenuState.SkillSelect)
        {
            HandleSkillNavigation();
        }
    }

    void HandleActionNavigation()
    {
        // [0: FIGHT] [2: ITEM]
        // [1: SKILL] [3: RUN]

        int previousIndex = currentActionIndex;

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            switch (currentActionIndex)
            {
                case 0: currentActionIndex = 2; break; // FIGHT -> ITEM
                case 1: currentActionIndex = 3; break; // SKILL -> RUN
            }
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            switch (currentActionIndex)
            {
                case 2: currentActionIndex = 0; break; // ITEM -> FIGHT
                case 3: currentActionIndex = 1; break; // RUN -> SKILL
            }
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            switch (currentActionIndex)
            {
                case 0: currentActionIndex = 1; break; // FIGHT -> SKILL
                case 2: currentActionIndex = 3; break; // ITEM -> RUN
            }
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            switch (currentActionIndex)
            {
                case 1: currentActionIndex = 0; break; // SKILL -> FIGHT
                case 3: currentActionIndex = 2; break; // RUN -> ITEM
            }
        }

        // If we moved, update the highlight
        if (previousIndex != currentActionIndex)
        {
            actionButtons[currentActionIndex].Select();
        }

        // Check for "Click"
        if (Input.GetKeyDown(KeyCode.Z))
        {
            actionButtons[currentActionIndex].onClick.Invoke();
        }
    }

    void HandleSkillNavigation()
    {
        int previousIndex = currentSkillIndex;
        int columns = 3; // The width of your skill grid

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentSkillIndex++;
            if (currentSkillIndex % columns == 0) currentSkillIndex = previousIndex;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentSkillIndex--;
            if (currentSkillIndex % columns == (columns - 1) || currentSkillIndex < 0) currentSkillIndex = previousIndex;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentSkillIndex += columns;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (currentSkillIndex >= columns)
            {
                currentSkillIndex -= columns;
            }
        }

        // Clamp values to the list of skills *we just created*
        currentSkillIndex = Mathf.Clamp(currentSkillIndex, 0, currentSkillButtons.Count - 1);

        if (previousIndex != currentSkillIndex)
        {
            // Deselect the old one
            currentSkillButtons[previousIndex].Deselect();
            // Select the new one
            currentSkillButtons[currentSkillIndex].Select();
        }

        // Check for "Click"
        if (Input.GetKeyDown(KeyCode.Z))
        {
            // We get the Button component *from* our script's GameObject
            currentSkillButtons[currentSkillIndex].GetComponent<Button>().onClick.Invoke();
        }
    }

    public void OnFightButton()
    {
        isPlayerTurn = false;
        SetButtonsInteractable(false);

        int roll = Random.Range(1, 21); // Roll a d20 (1 to 20)
        int finalRoll = roll + playerStats.luck;

        ShowMessage($"You rolled a {roll} ( +{playerStats.luck} luck ) = {finalRoll}");

        // --- Check the outcomes ---
        if (roll == 20)
        {
            // INSTA-KILL!
            PerformInstaKill(roll);
        }
        else if (finalRoll < 5)
        {
            // MISS
            PerformMiss(finalRoll);
        }
        else if (finalRoll >= 15)
        {
            // CRITICAL HIT
            PerformCritAttack(finalRoll);
        }
        else // Anything else (finalRoll between 5 and 14)
        {
            // NORMAL HIT
            PerformNormalAttack(finalRoll);
        }

        // After our turn, it's the enemy's turn
        // (We'll add a delay so the player can read the message)
        StartCoroutine(EnemyTurn());
    }

    void PerformNormalAttack(int finalRoll)
    {
        // Damage now includes the roll
        int damage = (playerStats.attack + finalRoll) - enemy.enemyData.defense;
        if (damage < 1) damage = 1;

        ShowMessage($"You attack! Your roll of {finalRoll} deals {damage} damage.");

        enemy.TakeDamage(damage);
        UpdateEnemyUI();
    }

    void PerformCritAttack(int finalRoll)
    {
        // Crit damage: 1.5x damage and ignores defense
        int damage = Mathf.RoundToInt((playerStats.attack + finalRoll) * 1.5f);
        if (damage < 1) damage = 1;

        ShowMessage($"A critical hit! Your roll of {finalRoll} deals {damage} damage!");

        enemy.TakeDamage(damage);
        UpdateEnemyUI();
    }

    void PerformInstaKill(int roll)
    {
        ShowMessage($"NATURAL 20! A devastating blow!");

        enemy.TakeDamage(999);
        UpdateEnemyUI();
    }

    void PerformMiss(int finalRoll)
    {
        ShowMessage($"You missed! (Your roll: {finalRoll})");
    }

    void ShowMessage(string message)
    {
        commentText.SetText(message);
    }

    IEnumerator EnemyTurn()
    {
        yield return new WaitForSeconds(2f);

        if (enemy.currentHealth > 0)
        {
            // --- ENEMY'S TURN LOGIC ---
            ShowMessage($"The {enemy.enemyData.enemyName} attacks...");
            yield return new WaitForSeconds(1.5f);

            int roll = Random.Range(1, 21);

            if (roll < 5)
            {
                // MISS
                ShowMessage($"The {enemy.enemyData.enemyName} rolled a {roll} and missed!");
            }
            else if (roll >= 15)
            {
                // CRITICAL HIT
                int damage = Mathf.RoundToInt((enemy.enemyData.attack + roll) * 1.5f) - playerStats.defense;
                if (damage < 1) damage = 1;

                playerStats.TakeDamage(damage);
                UpdatePlayerUI();
                ShowMessage($"A critical hit! The enemy rolled a {roll} and deals {damage} damage!");
            }
            else
            {
                // NORMAL HIT
                int damage = (enemy.enemyData.attack + roll) - playerStats.defense;
                if (damage < 1) damage = 1;

                playerStats.TakeDamage(damage);
                UpdatePlayerUI();
                ShowMessage($"The enemy rolled a {roll} and deals {damage} damage!");
            }

            yield return new WaitForSeconds(2f);
            SetButtonsInteractable(true);
            isPlayerTurn = true;
            actionButtons[currentActionIndex].Select();

        }
        else
        {
            ShowMessage($"You defeated the {enemy.enemyData.enemyName}!");

            int xpGained = enemy.enemyData.xpYield + Random.Range(0, 20);
            playerStats.GainXP(xpGained);

            yield return new WaitForSeconds(1.5f);

            ShowMessage($"You gained {xpGained} XP!");

            yield return new WaitForSeconds(2.5f);
            GameStatemanager.instance.EndBattle();
        }
    }

    public void OnSkillButton()
    {
        // 1. Get ONLY the battle skills from PlayerStats
        var battleSkills = playerStats.unlockedSkills.FindAll(
            skill => skill.skillType == SkillData.SkillType.BattleSkill
        );

        // 2. Check if we even have skills
        if (battleSkills.Count == 0)
        {
            ShowMessage("You have not learned any battle skills!");
            return;
        }

        // 3. We have skills! Switch the state.
        currentState = BattleMenuState.SkillSelect;
        skillListPanel.SetActive(true);
        SetButtonsInteractable(false);

        // 4. Clean up old buttons
        foreach (Transform child in skillRow1) { Destroy(child.gameObject); }
        foreach (Transform child in skillRow2) { Destroy(child.gameObject); }
        currentSkillButtons.Clear();

        // 5. Create new buttons and add them to the rows
        for (int i = 0; i < battleSkills.Count; i++)
        {
            SkillData currentSkill = battleSkills[i];

            Transform parentRow = (i < 3) ? skillRow1 : skillRow2;

            GameObject newButtonObj = Instantiate(skillButtonPrefab, parentRow);

            // Get the Button component
            Button newButton = newButtonObj.GetComponent<Button>();

            // Get our custom script
            SkillButton skillButtonScript = newButtonObj.GetComponent<SkillButton>();

            // Set its text (e.g., "Fire Slash   10 MP")
            skillButtonScript.Setup(currentSkill);

            // Add a listener to call OnSkillSelected
            newButton.onClick.AddListener(() => OnSkillSelected(currentSkill));

            // Add our custom script to the list
            currentSkillButtons.Add(skillButtonScript);
        }

        // 6. Select the first skill
        currentSkillIndex = 0;
        currentSkillButtons[0].Select();
    }

    void OnSkillSelected(SkillData selectedSkill)
    {
        // 1. Check for mana
        if (playerStats.currentMana < selectedSkill.manaCost)
        {
            ShowMessage("Not enough mana!");
            return; // Stay in the skill menu
        }

        // 2. Use the skill
        isPlayerTurn = false;
        SetButtonsInteractable(false); // Disable action buttons
        GoBackToActions();

        // 3. Spend the mana
        playerStats.UseMana(selectedSkill.manaCost);
        UpdatePlayerUI();

        // 4. TODO: Add logic for each skill (Heal, Fire Slash, etc.)
        ShowMessage($"You used {selectedSkill.skillName}!");

        // 5. Start the enemy's turn
        StartCoroutine(EnemyTurn());
    }

    void GoBackToActions()
    {
        currentState = BattleMenuState.ActionSelect;
        skillListPanel.SetActive(false); // Hide the skill grid
        SetButtonsInteractable(true);

        foreach (Transform child in skillRow1) { Destroy(child.gameObject); }
        foreach (Transform child in skillRow2) { Destroy(child.gameObject); }
        if (currentSkillButtons.Count > 0)
        {
            currentSkillButtons[currentSkillIndex].Deselect();
        }
        currentSkillButtons.Clear();

        currentActionIndex = 1;
        actionButtons[currentActionIndex].Select();
    }

    public void OnItemButton()
    {
        if (!isPlayerTurn) return;
        ShowMessage("You have no items!");
        // In the future, this will open the Item menu
    }

    public void OnRunButton()
    {
        // Don't let the player run if it's not their turn
        if (!isPlayerTurn) return;

        // Set state to prevent spamming
        isPlayerTurn = false;
        SetButtonsInteractable(false);

        // Tell the GameStatemanager to end the battle
        ShowMessage("You got away safely!");

        // We'll use a coroutine to add a small delay
        StartCoroutine(RunAway());
    }

    IEnumerator RunAway()
    {
        // Wait for the message to be read
        yield return new WaitForSeconds(1.5f);

        // This is the function we built in GameStatemanager
        GameStatemanager.instance.EndBattle();
    }

    void SetButtonsInteractable(bool state)
    {
        foreach (Button button in actionButtons)
        {
            button.interactable = state;
        }
    }
}
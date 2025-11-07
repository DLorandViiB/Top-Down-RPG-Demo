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

    private bool isWaitingForInput = false;

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
        if (isWaitingForInput) return;

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
        if (!isPlayerTurn) return;
        StartCoroutine(FightSequence());
    }

    IEnumerator FightSequence()
    {
        isPlayerTurn = false;
        SetButtonsInteractable(false);

        int roll = Random.Range(1, 21);
        int finalRoll = roll + playerStats.luck;

        yield return StartCoroutine(ShowMessageAndWait($"You rolled a {roll} ( +{playerStats.luck} luck ) = {finalRoll}"));

        // Check the outcomes
        if (roll == 20)
        {
            yield return StartCoroutine(PerformInstaKill(null));
        }
        else if (finalRoll < 5)
        {
            yield return StartCoroutine(PerformMiss(finalRoll));
        }
        else if (finalRoll >= 15)
        {
            yield return StartCoroutine(PerformCritAttack(finalRoll));
        }
        else
        {
            yield return StartCoroutine(PerformNormalAttack(finalRoll));
        }
    }

    IEnumerator PerformNormalAttack(int finalRoll)
    {
        int damage = (playerStats.attack + finalRoll) - enemy.enemyData.defense;
        if (damage < 1) damage = 1;

        yield return StartCoroutine(ShowMessageAndWait($"You attack! Your roll of {finalRoll} deals {damage} damage."));

        enemy.TakeDamage(damage);
        UpdateEnemyUI();
        StartCoroutine(EnemyTurn()); // Start the enemy's turn
    }

    IEnumerator PerformCritAttack(int finalRoll)
    {
        int damage = Mathf.RoundToInt((playerStats.attack + finalRoll) * 1.5f);
        if (damage < 1) damage = 1;

        yield return StartCoroutine(ShowMessageAndWait($"A critical hit! Your roll of {finalRoll} deals {damage} damage!"));

        enemy.TakeDamage(damage);
        UpdateEnemyUI();
        StartCoroutine(EnemyTurn());
    }

    // We can re-use the one from SkillData
    IEnumerator PerformInstaKill(SkillData skill)
    {
        if (skill != null)
            yield return StartCoroutine(ShowMessageAndWait($"You used {skill.skillName}... a killing blow!"));
        else
            yield return StartCoroutine(ShowMessageAndWait($"NATURAL 20! A devastating blow!"));

        if (enemy.enemyData.isBoss)
        {
            yield return StartCoroutine(ShowMessageAndWait("It has no effect on this powerful foe!"));
        }
        else
        {
            enemy.TakeDamage(9999);
            UpdateEnemyUI();
        }
        StartCoroutine(EnemyTurn());
    }

    IEnumerator PerformMiss(int finalRoll)
    {
        yield return StartCoroutine(ShowMessageAndWait($"You missed! (Your roll: {finalRoll})"));
        StartCoroutine(EnemyTurn());
    }

    IEnumerator ShowMessageAndWait(string message)
    {
        commentText.SetText(message);

        // TODO: You could show a little flashing "continue" arrow here

        isWaitingForInput = true;
        // Wait until the player presses the 'Z' key
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Z));
        isWaitingForInput = false;

        // TODO: You would hide the "continue" arrow here
    }

    IEnumerator EnemyTurn()
    {
        if (enemy.currentHealth > 0)
        {
            // --- ENEMY'S TURN LOGIC ---
            yield return StartCoroutine(ShowMessageAndWait($"The {enemy.enemyData.enemyName} attacks..."));

            yield return new WaitForSeconds(1.5f);

            int roll = Random.Range(1, 21);
            TakeDamageResult damageResult;

            if (roll < 5)
            {
                yield return StartCoroutine(ShowMessageAndWait($"The {enemy.enemyData.enemyName} rolled a {roll} and missed!"));
                // Create an empty result since no damage was dealt
                damageResult = new TakeDamageResult();
            }
            else // This block handles both Crits and Normal hits
            {
                int damage;
                string message;

                if (roll >= 15) // Crit
                {
                    damage = Mathf.RoundToInt((enemy.enemyData.attack + roll) * 1.5f) - playerStats.defense;
                    message = $"A critical hit! The enemy rolled a {roll} and deals";
                }
                else // Normal
                {
                    damage = (enemy.enemyData.attack + roll) - playerStats.defense;
                    message = $"The enemy rolled a {roll} and deals";
                }

                if (damage < 1) damage = 1;

                // 1. Take damage and get the results
                damageResult = playerStats.TakeDamage(damage);

                // 2. Update UI to show the DAMAGE
                UpdatePlayerUI();
                yield return StartCoroutine(ShowMessageAndWait($"{message} {damage} damage!"));
            }

            // Wait for the attack message to be read

            // 3. Check for and apply the HEAL (Guardian Angel)
            if (damageResult.healAmount > 0)
            {
                playerStats.Heal(damageResult.healAmount);
                UpdatePlayerUI(); // Update UI to show the heal
                yield return StartCoroutine(ShowMessageAndWait(damageResult.healMessage));
            }

            // 4. Check for and apply THORNS
            if (damageResult.thornsDamage > 0)
            {
                // Show message AND deal damage in the *same frame*
                enemy.TakeDamage(damageResult.thornsDamage);
                UpdateEnemyUI();

                // Now wait for the player to read the message
                yield return StartCoroutine(ShowMessageAndWait(damageResult.thornsMessage));

                if (enemy.currentHealth <= 0)
                {
                    StartCoroutine(EnemyDefeated());
                    yield break;
                }
            }

            playerStats.TickDownBuffs();

            SetButtonsInteractable(true);
            isPlayerTurn = true;
            actionButtons[currentActionIndex].Select();
        }
        else
        {
            StartCoroutine(EnemyDefeated());
        }
    }

    IEnumerator EnemyDefeated()
    {
        yield return StartCoroutine(ShowMessageAndWait($"You defeated the {enemy.enemyData.enemyName}!"));

        int xpGained = enemy.enemyData.xpYield + Random.Range(0, 20);
        playerStats.GainXP(xpGained);


        yield return StartCoroutine(ShowMessageAndWait($"You gained {xpGained} XP!"));

        GameStatemanager.instance.EndBattle();
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
            // Call the new coroutine
            StartCoroutine(ShowMessageAndWait("You have not learned any battle skills!"));
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

            Button newButton = newButtonObj.GetComponent<Button>();
            SkillButton skillButtonScript = newButtonObj.GetComponent<SkillButton>();

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
            StartCoroutine(ShowMessageAndWait("Not enough mana!"));
            return;
        }

        // Start the skill sequence
        StartCoroutine(SkillSequence(selectedSkill));
    }

    IEnumerator SkillSequence(SkillData selectedSkill)
    {
        isPlayerTurn = false;
        SetButtonsInteractable(false);
        currentState = BattleMenuState.ActionSelect;
        CleanupSkillList();

        playerStats.UseMana(selectedSkill.manaCost);
        UpdatePlayerUI();

        // Use the switch to decide what logic to run
        switch (selectedSkill.effect)
        {
            case SkillData.SkillEffect.Heal:
                yield return StartCoroutine(PerformHeal(selectedSkill));
                break;
            case SkillData.SkillEffect.GuaranteedRoll:
                yield return StartCoroutine(PerformDamage(selectedSkill, 15));
                break;
            case SkillData.SkillEffect.NormalDamage:
                yield return StartCoroutine(PerformDamage(selectedSkill, 1));
                break;
            case SkillData.SkillEffect.InstantKill:
                yield return StartCoroutine(PerformInstaKill(selectedSkill));
                break;
            case SkillData.SkillEffect.HealOnDamage:
                playerStats.AddBuff(selectedSkill);
                yield return StartCoroutine(ShowMessageAndWait($"You are protected by {selectedSkill.skillName}!"));
                StartCoroutine(EnemyTurn());
                break;
        }
    }

    void GoBackToActions()
    {
        currentState = BattleMenuState.ActionSelect;
        SetButtonsInteractable(true); // Re-enables the 2x2 grid
        CleanupSkillList(); // Call our new helper function

        // Re-select the "SKILL" button
        currentActionIndex = 1;
        actionButtons[currentActionIndex].Select();
    }

    public void OnItemButton()
    {
        if (!isPlayerTurn) return;
        StartCoroutine(ItemButtonSequence());
    }

    IEnumerator ItemButtonSequence()
    {
        yield return StartCoroutine(ShowMessageAndWait("You have no items!"));
    }

    public void OnRunButton()
    {
        if (!isPlayerTurn) return;
        StartCoroutine(RunAwaySequence());
    }

    IEnumerator RunAwaySequence()
    {
        isPlayerTurn = false;
        SetButtonsInteractable(false);

        yield return StartCoroutine(ShowMessageAndWait("You got away safely!"));

        GameStatemanager.instance.EndBattle();
    }

    void SetButtonsInteractable(bool state)
    {
        foreach (Button button in actionButtons)
        {
            button.interactable = state;
        }
    }

    void CleanupSkillList()
    {
        skillListPanel.SetActive(false); // Hide the skill grid

        // Deselect the last highlighted skill
        if (currentSkillButtons.Count > 0 && currentSkillIndex < currentSkillButtons.Count)
        {
            currentSkillButtons[currentSkillIndex].Deselect();
        }

        // Clean up the buttons
        foreach (Transform child in skillRow1) { Destroy(child.gameObject); }
        foreach (Transform child in skillRow2) { Destroy(child.gameObject); }
        currentSkillButtons.Clear();
    }

    // --- SKILL LOGIC HELPERS ---

    IEnumerator PerformHeal(SkillData skill)
    {
        int healAmount = Mathf.RoundToInt(playerStats.maxHealth * 0.3f);
        playerStats.Heal(healAmount);
        UpdatePlayerUI();
        yield return StartCoroutine(ShowMessageAndWait($"You used {skill.skillName} and healed {healAmount} HP!"));
        StartCoroutine(EnemyTurn());
    }

    IEnumerator PerformInstantKill(SkillData skill)
    {
        if (enemy.enemyData.isBoss)
        {
            yield return StartCoroutine(ShowMessageAndWait("It has no effect on this powerful foe!"));
        }
        else
        {
            yield return StartCoroutine(ShowMessageAndWait($"You used {skill.skillName}... a killing blow!"));
            enemy.TakeDamage(9999); // Insta-kill
            UpdateEnemyUI();
        }

        // Start the enemy's turn (even if it's dead, to process the win)
        StartCoroutine(EnemyTurn());
    }

    IEnumerator PerformDamage(SkillData skill, int minRoll)
    {
        // 1. Get the roll
        int roll = Random.Range(1, 21);

        // Check for "Heavy Blow"
        if (minRoll > 1)
        {
            roll = Mathf.Max(roll, minRoll); // Guarantees a 15 or higher roll
            yield return StartCoroutine(ShowMessageAndWait($"You used {skill.skillName} for a guaranteed heavy hit!"));
        }
        else
        {
            yield return StartCoroutine(ShowMessageAndWait($"You used {skill.skillName}!"));
        }

        int finalRoll = roll + playerStats.luck;

        int attackBonus = 0;
        foreach (Buff buff in playerStats.activeBuffs)
        {
            if (buff.effect == SkillData.SkillEffect.BuffAttack)
            {
                attackBonus = 10;
            }
        }

        // 2. Calculate base damage
        int damage = (playerStats.attack + attackBonus + finalRoll) - enemy.enemyData.defense;
        if (damage < 1) damage = 1;

        // 3. Check for elemental weakness/resistance
        float multiplier = 1.0f;
        string effectMessage = "";

        // Check Fire vs Ice
        if (skill.element == SkillData.ElementType.Fire && enemy.enemyData.element == EnemyData.ElementType.Ice)
        {
            multiplier = 2.0f;
            effectMessage = $"The {enemy.enemyData.enemyName} takes heavy damage!";
        }
        // Check Ice vs Fire
        else if (skill.element == SkillData.ElementType.Ice && enemy.enemyData.element == EnemyData.ElementType.Fire)
        {
            multiplier = 2.0f;
            effectMessage = $"The {enemy.enemyData.enemyName} takes heavy damage!";
        }

        // Check Fire vs Fire
        if (skill.element == SkillData.ElementType.Fire && enemy.enemyData.element == EnemyData.ElementType.Fire)
        {
            multiplier = 0.5f;
            effectMessage = $"The {enemy.enemyData.enemyName} takes light damage!";
        }
        // Check Ice vs Ice
        else if (skill.element == SkillData.ElementType.Ice && enemy.enemyData.element == EnemyData.ElementType.Ice)
        {
            multiplier = 0.5f;
            effectMessage = $"The {enemy.enemyData.enemyName} takes light damage!";
        }

        // 4. Apply multiplier
        damage = Mathf.RoundToInt(damage * multiplier);

        // 5. Deal damage and show message
        enemy.TakeDamage(damage);
        UpdateEnemyUI();

        // We'll show the message in a coroutine to add a delay
        StartCoroutine(ShowDamageMessage(finalRoll, damage, effectMessage));
    }

    IEnumerator ShowDamageMessage(int finalRoll, int damage, string effectMessage)
    {
        yield return StartCoroutine(ShowMessageAndWait($"Your roll of {finalRoll} deals {damage} damage!{effectMessage}"));
        StartCoroutine(EnemyTurn());
    }
}
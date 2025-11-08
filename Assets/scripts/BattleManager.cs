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
    public TextMeshProUGUI commentText;
    public Image continueArrow;

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

    // --- Action Queue ---
    private Queue<IEnumerator> battleActionQueue = new Queue<IEnumerator>();
    private bool isSequenceRunning = false;
    // --- End Action Queue ---

    private bool isWaitingForInput = false;

    // This is the "brain" of the battle. It runs forever.
    IEnumerator RunBattleQueue()
    {
        while (true)
        {
            if (battleActionQueue.Count > 0)
            {
                isSequenceRunning = true; // Lock input
                yield return StartCoroutine(battleActionQueue.Dequeue()); // Run the next action and wait
            }
            else
            {
                isSequenceRunning = false; // Unlock input
                yield return null; // Wait a frame
            }
        }
    }

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

        StartCoroutine(RunBattleQueue()); // Start the "brain"
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
    }

    void Update()
    {
        if (isWaitingForInput) return;
        if (isSequenceRunning) return; // Lock input if the queue is running
        if (!isPlayerTurn) return;

        if (currentState == BattleMenuState.SkillSelect && Input.GetKeyDown(KeyCode.X))
        {
            GoBackToActions();
            return;
        }

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

        if (previousIndex != currentActionIndex)
        {
            actionButtons[currentActionIndex].Select();
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            actionButtons[currentActionIndex].onClick.Invoke();
        }
    }

    void HandleSkillNavigation()
    {
        int previousIndex = currentSkillIndex;
        int columns = 3;

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

        currentSkillIndex = Mathf.Clamp(currentSkillIndex, 0, currentSkillButtons.Count - 1);

        if (previousIndex != currentSkillIndex && currentSkillButtons.Count > 0)
        {
            currentSkillButtons[previousIndex].Deselect();
            currentSkillButtons[currentSkillIndex].Select();
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            currentSkillButtons[currentSkillIndex].GetComponent<Button>().onClick.Invoke();
        }
    }

    IEnumerator ShowMessageAndWait(string message)
    {
        commentText.SetText(message);
        continueArrow.gameObject.SetActive(true);
        Coroutine flashRoutine = StartCoroutine(FlashContinueArrow());

        isWaitingForInput = true;
        yield return null;
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Z));
        isWaitingForInput = false;

        StopCoroutine(flashRoutine);
        continueArrow.gameObject.SetActive(false);
    }

    IEnumerator FlashContinueArrow()
    {
        while (true)
        {
            continueArrow.enabled = !continueArrow.enabled;
            yield return new WaitForSeconds(0.4f);
        }
    }

    // --- ENEMY TURN & DEFEAT LOGIC ---

    IEnumerator EnemyTurn()
    {
        // Check if the enemy was already dead
        if (enemy.currentHealth <= 0)
        {
            battleActionQueue.Enqueue(EnemyDefeated()); // Queue the defeat
            yield break; // Stop this turn
        }

        yield return StartCoroutine(ShowMessageAndWait($"The {enemy.enemyData.enemyName} attacks..."));

        int roll = Random.Range(1, 21);
        TakeDamageResult damageResult;

        if (roll < 5)
        {
            yield return StartCoroutine(ShowMessageAndWait($"The {enemy.enemyData.enemyName} rolled a {roll} and missed!"));
            damageResult = new TakeDamageResult();
        }
        else
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

            damageResult = playerStats.TakeDamage(damage);
            UpdatePlayerUI();
            yield return StartCoroutine(ShowMessageAndWait($"{message} {damage} damage!"));
        }

        if (damageResult.healAmount > 0)
        {
            playerStats.Heal(damageResult.healAmount);
            UpdatePlayerUI();
            yield return StartCoroutine(ShowMessageAndWait(damageResult.healMessage));
        }

        if (damageResult.thornsDamage > 0)
        {
            enemy.TakeDamage(damageResult.thornsDamage);
            UpdateEnemyUI();
            yield return StartCoroutine(ShowMessageAndWait(damageResult.thornsMessage));

            if (enemy.currentHealth <= 0)
            {
                battleActionQueue.Enqueue(EnemyDefeated());
                yield break;
            }
        }

        // --- END OF TURN ---
        playerStats.TickDownBuffs();
        SetButtonsInteractable(true);
        isPlayerTurn = true;
        actionButtons[currentActionIndex].Select();
    }

    IEnumerator EnemyDefeated()
    {
        yield return StartCoroutine(ShowMessageAndWait($"You defeated the {enemy.enemyData.enemyName}!"));

        int xpGained = enemy.enemyData.xpYield + Random.Range(0, 20);
        playerStats.GainXP(xpGained);

        yield return StartCoroutine(ShowMessageAndWait($"You gained {xpGained} XP!"));
        GameStatemanager.instance.EndBattle();
    }

    // --- PLAYER ACTION LOGIC (BUTTON CLICKS) ---

    public void OnFightButton()
    {
        if (!isPlayerTurn || isSequenceRunning) return;
        isPlayerTurn = false;
        SetButtonsInteractable(false);
        battleActionQueue.Enqueue(FightSequence());
    }

    IEnumerator FightSequence()
    {
        int roll = Random.Range(1, 21);
        int finalRoll = roll + playerStats.luck;

        yield return StartCoroutine(ShowMessageAndWait($"You rolled a {roll} ( +{playerStats.luck} luck ) = {finalRoll}"));

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

        battleActionQueue.Enqueue(EnemyTurn());
    }

    public void OnSkillButton()
    {
        var battleSkills = playerStats.unlockedSkills.FindAll(
            skill => skill.skillType == SkillData.SkillType.BattleSkill
        );

        if (battleSkills.Count == 0)
        {
            StartCoroutine(ShowMessageAndWait("You have not learned any battle skills!"));
            return;
        }

        currentState = BattleMenuState.SkillSelect;
        skillListPanel.SetActive(true);
        SetButtonsInteractable(false);

        foreach (Transform child in skillRow1) { Destroy(child.gameObject); }
        foreach (Transform child in skillRow2) { Destroy(child.gameObject); }
        currentSkillButtons.Clear();

        for (int i = 0; i < battleSkills.Count; i++)
        {
            SkillData currentSkill = battleSkills[i];
            Transform parentRow = (i < 3) ? skillRow1 : skillRow2;
            GameObject newButtonObj = Instantiate(skillButtonPrefab, parentRow);

            Button newButton = newButtonObj.GetComponent<Button>();
            SkillButton skillButtonScript = newButtonObj.GetComponent<SkillButton>();

            skillButtonScript.Setup(currentSkill);
            newButton.onClick.AddListener(() => OnSkillSelected(currentSkill));
            currentSkillButtons.Add(skillButtonScript);
        }

        currentSkillIndex = 0;
        currentSkillButtons[0].Select();
    }

    void OnSkillSelected(SkillData selectedSkill)
    {
        if (playerStats.currentMana < selectedSkill.manaCost)
        {
            StartCoroutine(ShowMessageAndWait("Not enough mana!"));
            return;
        }

        isPlayerTurn = false;
        SetButtonsInteractable(false);
        currentState = BattleMenuState.ActionSelect;
        CleanupSkillList();

        battleActionQueue.Enqueue(SkillSequence(selectedSkill));
    }

    IEnumerator SkillSequence(SkillData selectedSkill)
    {
        playerStats.UseMana(selectedSkill.manaCost);
        UpdatePlayerUI();

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
                Buff newBuff = new Buff();
                newBuff.effect = selectedSkill.effect;
                newBuff.duration = selectedSkill.buffDuration;
                playerStats.AddBuff(newBuff);
                yield return StartCoroutine(ShowMessageAndWait($"You are protected by {selectedSkill.skillName}!"));
                break;
            case SkillData.SkillEffect.Thorns:
                Buff thornsBuff = new Buff();
                thornsBuff.effect = selectedSkill.effect;
                thornsBuff.duration = selectedSkill.buffDuration;
                playerStats.AddBuff(thornsBuff);
                yield return StartCoroutine(ShowMessageAndWait($"You are protected by {selectedSkill.skillName}!"));
                break;
        }

        battleActionQueue.Enqueue(EnemyTurn());
    }

    public void OnItemButton()
    {
        if (!isPlayerTurn || isSequenceRunning) return;
        isPlayerTurn = false;
        SetButtonsInteractable(false);
        battleActionQueue.Enqueue(ItemButtonSequence());
    }

    IEnumerator ItemButtonSequence()
    {
        yield return StartCoroutine(ShowMessageAndWait("You have no items!"));
        battleActionQueue.Enqueue(EnemyTurn()); // Using an item still costs a turn
    }

    public void OnRunButton()
    {
        if (!isPlayerTurn || isSequenceRunning) return;
        isPlayerTurn = false;
        SetButtonsInteractable(false);
        battleActionQueue.Enqueue(RunAwaySequence());
    }

    IEnumerator RunAwaySequence()
    {
        yield return StartCoroutine(ShowMessageAndWait("You got away safely!"));
        GameStatemanager.instance.EndBattle();
    }

    void GoBackToActions()
    {
        currentState = BattleMenuState.ActionSelect;
        SetButtonsInteractable(true);
        CleanupSkillList();

        currentActionIndex = 1;
        actionButtons[currentActionIndex].Select();
    }

    void SetButtonsInteractable(bool state)
    {
        Navigation newNav = new Navigation();
        newNav.mode = state ? Navigation.Mode.Automatic : Navigation.Mode.None;

        foreach (Button button in actionButtons)
        {
            button.interactable = state;
            button.navigation = newNav;
        }
    }

    void CleanupSkillList()
    {
        skillListPanel.SetActive(false);

        if (currentSkillButtons.Count > 0 && currentSkillIndex < currentSkillButtons.Count)
        {
            currentSkillButtons[currentSkillIndex].Deselect();
        }

        foreach (Transform child in skillRow1) { Destroy(child.gameObject); }
        foreach (Transform child in skillRow2) { Destroy(child.gameObject); }
        currentSkillButtons.Clear();
    }

    // --- SKILL LOGIC HELPERS ---

    // These functions (PerformHeal, PerformDamage, etc.) are the
    // missing pieces from your last script.

    IEnumerator PerformHeal(SkillData skill)
    {
        int healAmount = Mathf.RoundToInt(playerStats.maxHealth * 0.3f);
        playerStats.Heal(healAmount);
        UpdatePlayerUI();
        yield return StartCoroutine(ShowMessageAndWait($"You used {skill.skillName} and healed {healAmount} HP!"));
    }

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
    }

    IEnumerator PerformMiss(int finalRoll)
    {
        yield return StartCoroutine(ShowMessageAndWait($"You missed! (Your roll: {finalRoll})"));
    }

    IEnumerator PerformDamage(SkillData skill, int minRoll)
    {
        int roll = Random.Range(1, 21);

        if (minRoll > 1)
        {
            roll = Mathf.Max(roll, minRoll);
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

        int damage = (playerStats.attack + attackBonus + finalRoll) - enemy.enemyData.defense;
        if (damage < 1) damage = 1;

        float multiplier = 1.0f;
        string effectMessage = "";

        if (skill.element == SkillData.ElementType.Fire && enemy.enemyData.element == EnemyData.ElementType.Ice)
        {
            multiplier = 2.0f;
            effectMessage = $" The {enemy.enemyData.enemyName} takes heavy damage!";
        }
        else if (skill.element == SkillData.ElementType.Ice && enemy.enemyData.element == EnemyData.ElementType.Fire)
        {
            multiplier = 2.0f;
            effectMessage = $" The {enemy.enemyData.enemyName} takes heavy damage!";
        }
        else if (skill.element == SkillData.ElementType.Fire && enemy.enemyData.element == EnemyData.ElementType.Fire)
        {
            multiplier = 0.5f;
            effectMessage = $" The {enemy.enemyData.enemyName} takes light damage!";
        }
        else if (skill.element == SkillData.ElementType.Ice && enemy.enemyData.element == EnemyData.ElementType.Ice)
        {
            multiplier = 0.5f;
            effectMessage = $" The {enemy.enemyData.enemyName} takes light damage!";
        }

        damage = Mathf.RoundToInt(damage * multiplier);
        enemy.TakeDamage(damage);
        UpdateEnemyUI();

        yield return StartCoroutine(ShowDamageMessage(finalRoll, damage, effectMessage));
    }

    IEnumerator ShowDamageMessage(int finalRoll, int damage, string effectMessage)
    {
        yield return StartCoroutine(ShowMessageAndWait($"Your roll of {finalRoll} deals {damage} damage!{effectMessage}"));
    }

    IEnumerator PerformNormalAttack(int finalRoll)
    {
        int damage = (playerStats.attack + finalRoll) - enemy.enemyData.defense;
        if (damage < 1) damage = 1;
        yield return StartCoroutine(ShowMessageAndWait($"You attack! Your roll of {finalRoll} deals {damage} damage."));
        enemy.TakeDamage(damage);
        UpdateEnemyUI();
    }

    IEnumerator PerformCritAttack(int finalRoll)
    {
        int damage = Mathf.RoundToInt((playerStats.attack + finalRoll) * 1.5f);
        if (damage < 1) damage = 1;
        yield return StartCoroutine(ShowMessageAndWait($"A critical hit! Your roll of {finalRoll} deals {damage} damage!"));
        enemy.TakeDamage(damage);
        UpdateEnemyUI();
    }
}
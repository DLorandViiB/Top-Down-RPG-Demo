using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BattleManager : MonoBehaviour
{
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
    private int selectedButtonIndex = 0;
    private bool isPlayerTurn = true;

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

        actionButtons[selectedButtonIndex].Select();
        actionButtons[0].onClick.AddListener(OnFightButton);
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
        if (isPlayerTurn == false)
        {
            return;
        }

        // --- Button Navigation ---
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            selectedButtonIndex++;
            if (selectedButtonIndex >= actionButtons.Length)
            {
                selectedButtonIndex = 0;
            }
            actionButtons[selectedButtonIndex].Select();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            selectedButtonIndex--;
            if (selectedButtonIndex < 0)
            {
                selectedButtonIndex = actionButtons.Length - 1;
            }
            actionButtons[selectedButtonIndex].Select();
        }

        // --- Button "Click" ---
        if (Input.GetKeyDown(KeyCode.Z))
        {
            actionButtons[selectedButtonIndex].onClick.Invoke();
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
            actionButtons[selectedButtonIndex].Select();

        }
        else
        {
            ShowMessage($"You defeated the {enemy.enemyData.enemyName}!");
            playerStats.GainXP(enemy.enemyData.xpYield + Random.Range(0,20));

            yield return new WaitForSeconds(1.5f);

            ShowMessage($"You gained {enemy.enemyData.xpYield + Random.Range(0, 20)} XP!");

            yield return new WaitForSeconds(2.5f);

            GameStatemanager.instance.EndBattle();
        }
    }

    void SetButtonsInteractable(bool state)
    {
        foreach (Button button in actionButtons)
        {
            button.interactable = state;
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    void Start()
    {
        // --- 1. FIND OBJECTS (This is where the error happens) ---
        playerStats = PlayerStats.instance; // This line needs PlayerStats.Awake() to have run first!
        enemy = FindFirstObjectByType<EnemyBattleController>();


        // --- 2. HIDE PLAYER SPRITE ---
        if (playerStats != null)
        {
            SpriteRenderer playerSprite = playerStats.GetComponentInChildren<SpriteRenderer>();
            if (playerSprite != null)
            {
                playerSprite.enabled = false;
            }
        }

        // --- 3. SETUP ENEMY (This is the new logic) ---
        if (GameStatemanager.instance != null && GameStatemanager.instance.enemyToBattle != null)
        {
            // Normal path: Get the enemy from the GameStatemanager
            enemy.Setup(GameStatemanager.instance.enemyToBattle);
        }
        else
        {
            // Failsafe path: We're testing the scene directly
            enemy.Setup(null); // The 'null' will trigger the placeholder logic
        }

        // --- 4. UPDATE UI ---
        UpdatePlayerUI(); // This needs playerStats to be not-null
        UpdateEnemyUI();  // This needs enemy to be not-null

        if (GameStatemanager.instance != null)
        {
            StartCoroutine(GameStatemanager.instance.FadeIn());
        }
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
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleManager : MonoBehaviour
{
    [Header("Player")]
    private PlayerStats playerStats;

    [Header("Enemy")]
    public EnemyBattleController enemy;

    [Header("UI References")]
    public Slider playerHealthBar;
    public TextMeshProUGUI playerHPText;
    public Slider playerManaBar;
    public TextMeshProUGUI playerMPText;

    public Slider enemyHealthBar;

    void Start()
    {
        playerStats = FindObjectOfType<PlayerStats>();

        enemy.Setup("Placeholder", 50);

        UpdatePlayerUI();
        UpdateEnemyUI();
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
        enemyHealthBar.maxValue = enemy.maxHealth;
        enemyHealthBar.value = enemy.currentHealth;
    }
}
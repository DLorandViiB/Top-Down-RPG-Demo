using UnityEngine;
using TMPro;

public class CharacterMenuController : MonoBehaviour
{
    public PlayerStats playerStats;

    public TMP_Text nameText;
    public TMP_Text attackText;
    public TMP_Text defenseText;
    public TMP_Text luckText;
    public TMP_Text levelText;

    private void Start()
    {
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats not assigned to StatsUI!");
            return;
        }

        playerStats.OnStatsChanged += UpdateStats;

        UpdateStats();
    }

    private void OnDestroy()
    {
        if (playerStats != null)
            playerStats.OnStatsChanged -= UpdateStats;
    }

    public void UpdateStats()
    {
        nameText.text = playerStats.playerName;
        attackText.text = "Attack: " + playerStats.attack;
        defenseText.text = "Defense: " + playerStats.defense;
        luckText.text = "Luck: " + playerStats.luck;
        levelText.text = "Level: " + playerStats.level;
    }
}

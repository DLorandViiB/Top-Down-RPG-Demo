using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats")]
    public int level = 1;
    public int maxHealth = 100;
    public int currentHealth;
    public int maxMana = 50;
    public int currentMana;
    public int attack = 10;
    public int defense = 5;
    public int luck = 2;

    [Header("Experience")]
    public int currentXP = 0;
    public int xpToNextLevel = 100;

    private void Start()
    {
        currentHealth = maxHealth;
        currentMana = maxMana;
    }

    public void TakeDamage(int damage)
    {
        int finalDamage = Mathf.Max(damage - defense, 1);
        currentHealth -= finalDamage;
        if (currentHealth < 0) currentHealth = 0;
    }

    public void GainXP(int amount)
    {
        currentXP += amount;
        if (currentXP >= xpToNextLevel)
            LevelUp();
    }

    private void LevelUp()
    {
        level++;
        currentXP -= xpToNextLevel;
        xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * 1.2f);
        maxHealth += 10;
        maxMana += 5;
        attack += 2;
        defense += 1;
        luck += 1;
        currentHealth = maxHealth;
        currentMana = maxMana;
    }
}

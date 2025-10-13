using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats")]
    public string playerName = "Hero";
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

    public event Action OnStatsChanged;

    private void Start()
    {
        currentHealth = maxHealth;
        currentMana = maxMana;

        OnStatsChanged?.Invoke();
    }

    public void TakeDamage(int damage)
    {
        int finalDamage = Mathf.Max(damage - defense, 1);
        currentHealth -= finalDamage;
        if (currentHealth < 0) currentHealth = 0;

        OnStatsChanged?.Invoke();
    }

    public void GainXP(int amount)
    {
        currentXP += amount;
        if (currentXP >= xpToNextLevel)
        {
            LevelUp();
        }

        OnStatsChanged?.Invoke();
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

        OnStatsChanged?.Invoke();
    }

    public void UseMana(int amount)
    {
        currentMana = Mathf.Max(currentMana - amount, 0);
        OnStatsChanged?.Invoke();
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnStatsChanged?.Invoke();
    }
}

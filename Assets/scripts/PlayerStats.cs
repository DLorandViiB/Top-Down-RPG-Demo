using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats instance;

    //Singleton
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

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
    public int skillPoints = 0;

    [Header("Experience")]
    public int currentXP = 0;
    public int xpToNextLevel = 100;

    [Header("Skills")]
    public List<SkillData> unlockedSkills = new List<SkillData>();

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
        skillPoints++;

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

    // Call this from the UI to try and buy a skill
    public bool UnlockSkill(SkillData skillToUnlock)
    {
        // 1. Check if we can afford it
        if (skillPoints < skillToUnlock.skillPointCost)
        {
            Debug.Log("Not enough skill points!");
            return false;
        }

        // 2. Check if we already have it
        if (unlockedSkills.Contains(skillToUnlock))
        {
            Debug.Log("Already unlocked this skill!");
            return false;
        }

        // 3. Purchase the skill
        skillPoints -= skillToUnlock.skillPointCost;
        unlockedSkills.Add(skillToUnlock);

        // 4. Apply passive stat boosts immediately
        if (skillToUnlock.skillType == SkillData.SkillType.PassiveStatBoost)
        {
            ApplyPassiveStat(skillToUnlock);
        }

        // 5. This will force all UI to update.
        OnStatsChanged?.Invoke();
        return true;
    }

    // A private helper function to apply stats
    private void ApplyPassiveStat(SkillData skill)
    {
        if (skill.statToBoost == SkillData.StatToBoost.MaxHealth)
        {
            maxHealth += skill.boostAmount;
            Heal(skill.boostAmount); // Also heal the player
        }
        else if (skill.statToBoost == SkillData.StatToBoost.MaxMana)
        {
            maxMana += skill.boostAmount;
            currentMana = maxMana;
        }
    }
}

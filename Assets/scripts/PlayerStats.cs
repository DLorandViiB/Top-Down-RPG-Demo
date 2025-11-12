using UnityEngine;
using System;
using System.Collections.Generic;

public struct TakeDamageResult
{
    // We'll store the specific message for each effect
    public string thornsMessage;
    public int thornsDamage;

    public string healMessage;
    public int healAmount;

    public string guardianAngelMessage;
    public bool didGuardianAngelProc;
}

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats instance;

    //Singleton
    private void Awake()
    {
        // This just sets the instance. 
        // GameStatemanager handles persistence.
        if (instance == null)
        {
            instance = this;
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
    public List<Buff> activeBuffs = new List<Buff>();

    // This flag is reset by BattleManager at the start of a fight
    public bool hasGuardianAngelCastThisBattle = false;

    public event Action OnStatsChanged;

    private void Start()
    {
        currentHealth = maxHealth;
        currentMana = maxMana;

        OnStatsChanged?.Invoke();
    }

    public void PrepareForBattle()
    {
        this.hasGuardianAngelCastThisBattle = false;
    }

    public TakeDamageResult TakeDamage(int damage)
    {
        int finalDamage = Mathf.Max(damage - defense, 1);
        currentHealth -= finalDamage;

        TakeDamageResult result = CheckBuffsOnDamage(finalDamage);

        if (this.currentHealth <= 0)
        {
            // Player's health has hit 0. Check for a "death save".
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                if (activeBuffs[i].effect == SkillData.SkillEffect.GuardianAngel)
                {
                    // Heal to 30%
                    int healAmount = Mathf.RoundToInt(this.maxHealth * 0.3f);
                    this.currentHealth = healAmount;

                    // Send this info back to BattleManager so it can show a message
                    result.guardianAngelMessage = $"Guardian Angel saves you! You are restored to {healAmount} HP!";
                    result.didGuardianAngelProc = true;

                    // Remove the buff so it only procs once
                    activeBuffs.RemoveAt(i);
                    break;
                }
            }

            // If we're still at 0 health (no buff found), player is defeated.
            if (this.currentHealth <= 0)
            {
                this.currentHealth = 0;
            }
        }

        OnStatsChanged?.Invoke();

        return result;
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

    public void RestoreMana(int amount)
    {
        currentMana = Mathf.Min(currentMana + amount, maxMana);
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

    public void ApplyPassiveStat(SkillData skill)
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

    // BattleManager will call this to add the buff
    public void AddBuff(Buff newBuff)
    {
        // Check if a buff of this type already exists
        foreach (Buff buff in activeBuffs)
        {
            if (buff.effect == newBuff.effect)
            {
                buff.duration = newBuff.duration;
                OnStatsChanged?.Invoke();
                return;
            }
        }
        // If not, add the new one
        activeBuffs.Add(newBuff);
        OnStatsChanged?.Invoke();
    }

    // BattleManager will call this at the END of the player's turn
    public void TickDownBuffs()
    {
        // Loop backwards so we can safely remove items
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            activeBuffs[i].duration--;
            if (activeBuffs[i].duration <= 0)
            {
                // Buff has expired
                activeBuffs.RemoveAt(i);
            }
        }
    }

    // This is a private helper, called by TakeDamage
    private TakeDamageResult CheckBuffsOnDamage(int damageTaken)
    {
        TakeDamageResult result = new TakeDamageResult();
        // Initialize all fields
        result.thornsMessage = "";
        result.healMessage = "";
        result.guardianAngelMessage = "";
        result.didGuardianAngelProc = false;

        foreach (Buff buff in activeBuffs)
        {
            if (buff.effect == SkillData.SkillEffect.HealOnDamage)
            {
                int healAmount = 10;
                result.healAmount = healAmount;
                result.healMessage = $"Your 'Heal' restores {healAmount} HP!";
            }

            if (buff.effect == SkillData.SkillEffect.Thorns)
            {
                int thornDamage = 15;
                result.thornsDamage = thornDamage;
                result.thornsMessage = $"Your thorns deal {thornDamage} damage to the enemy!";
            }
        }
        return result;
    }

    public void ClearAllBuffs()
    {
        activeBuffs.Clear();
    }

    public void NotifyStatsChanged()
    {
        OnStatsChanged?.Invoke();
    }
}
using UnityEngine;
using System;
using System.Collections.Generic;

public struct TakeDamageResult
{
    public List<string> messages;
    public int thornsDamage;
    public int healAmount;
}

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
    public List<Buff> activeBuffs = new List<Buff>();

    public event Action OnStatsChanged;

    private void Start()
    {
        currentHealth = maxHealth;
        currentMana = maxMana;

        OnStatsChanged?.Invoke();
    }

    public TakeDamageResult TakeDamage(int damage)
    {
        int finalDamage = Mathf.Max(damage - defense, 1);
        currentHealth -= finalDamage;
        if (currentHealth < 0) currentHealth = 0;

        OnStatsChanged?.Invoke();

        // Check for buffs and get the result
        TakeDamageResult result = CheckBuffsOnDamage(finalDamage);
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

    // BattleManager will call this to add the buff
    public void AddBuff(SkillData skill)
    {
        // 1. Check if a buff of this type already exists
        foreach (Buff buff in activeBuffs)
        {
            if (buff.effect == skill.effect)
            {
                // 2. It exists! Reset its duration.
                buff.duration = skill.buffDuration;
                Debug.Log($"Buff '{skill.skillName}' duration reset to {skill.buffDuration}.");
                OnStatsChanged?.Invoke();
                return;
            }
        }

        // 3. If we're here, it's a new buff. Add it normally.
        Buff newBuff = new Buff();
        newBuff.effect = skill.effect;
        newBuff.duration = skill.buffDuration;

        activeBuffs.Add(newBuff);
        OnStatsChanged?.Invoke();
    }

    public void AddBuff(Buff newBuff)
    {
        // Check if a buff of this type already exists
        foreach (Buff buff in activeBuffs)
        {
            if (buff.effect == newBuff.effect)
            {
                buff.duration = newBuff.duration;
                return;
            }
        }
        // If not, add the new one
        activeBuffs.Add(newBuff);
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
        result.messages = new List<string>();
        result.thornsDamage = 0;
        result.healAmount = 0; // <-- ADD THIS

        foreach (Buff buff in activeBuffs)
        {
            if (buff.effect == SkillData.SkillEffect.HealOnDamage)
            {
                int healAmount = 10;
                result.healAmount = healAmount;
                result.messages.Add($"Guardian Angel heals you for {healAmount} HP!");
            }
            if (buff.effect == SkillData.SkillEffect.Thorns)
            {
                int thornDamage = 15;
                result.thornsDamage = thornDamage;
                result.messages.Add($"Your thorns deal {thornDamage} damage to the enemy!");
            }
        }
        return result;
    }

    public void ClearAllBuffs()
    {
        activeBuffs.Clear();
    }
}

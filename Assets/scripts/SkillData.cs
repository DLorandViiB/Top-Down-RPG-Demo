using UnityEngine;

// This lets us create new Skill assets from the Assets menu
[CreateAssetMenu(fileName = "New Skill", menuName = "RPG/Skill")]
public class SkillData : ScriptableObject
{
    // --- We define all our Enums at the top ---
    public enum SkillType { BattleSkill, PassiveStatBoost }
    public enum StatToBoost { None, MaxHealth, MaxMana }
    public enum ElementType { None, Fire, Ice }

    public enum SkillEffect { NormalDamage, Heal, GuaranteedRoll, InstantKill, HealOnDamage }

    // --- Then we define all our fields (variables) ---
    public string skillName;
    [TextArea(3, 10)] // Makes the description box bigger in the Inspector
    public string description;

    public SkillType skillType;

    [Header("Cost")]
    public int skillPointCost = 1;

    [Header("Passive Stat Boost")] // This is now correctly placed
    public StatToBoost statToBoost;
    public int boostAmount;

    [Header("Battle Skill")] // This is also correctly placed
    public int manaCost;
    public ElementType element;
    public SkillEffect effect;
    public int buffDuration;
}
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "RPG/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    [TextArea(3, 10)]
    public string description;
    public Sprite icon;
    public int price = 10;

    // We'll use this to decide what logic to run
    public enum ItemEffect
    {
        HealHP,
        HealMP,
        BuffAttack,
        Thorns
    }

    public ItemEffect effect;
    public int amount; // e.g., Heal 50 HP, or 3 turns

    [Header("Permissions")]
    public bool canUseInBattle = true;
    public bool canUseInMenu = true;

    [Header("Stacking")]
    public bool isStackable = true;
    public int maxStackSize = 10;
}
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int quantity;

    public InventorySlot()
    {
        item = null;
        quantity = 0;
    }
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance;

    public List<InventorySlot> slots = new List<InventorySlot>();
    public int totalSlots = 24;

    [Header("Starting Items")]
    public List<ItemData> startingItems;

    // This event will tell the UI to redraw itself
    public event System.Action OnInventoryChanged;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);

            for (int i = 0; i < totalSlots; i++)
            {
                slots.Add(new InventorySlot());
            }
            foreach (ItemData item in startingItems)
            {
                AddItem(item);
            }
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    public bool AddItem(ItemData itemToAdd)
    {
        // 1. Try to stack with existing items
        if (itemToAdd.isStackable)
        {
            foreach (InventorySlot slot in slots)
            {
                // Do we already have this item, and is the stack not full?
                if (slot.item == itemToAdd && slot.quantity < itemToAdd.maxStackSize)
                {
                    slot.quantity++;
                    OnInventoryChanged?.Invoke(); // Tell the UI to update
                    return true;
                }
            }
        }

        // 2. If no stacks, find the first empty slot
        foreach (InventorySlot slot in slots)
        {
            if (slot.item == null)
            {
                slot.item = itemToAdd;
                slot.quantity = 1;
                OnInventoryChanged?.Invoke(); // Tell the UI to update
                return true;
            }
        }

        // 3. If no stacks and no empty slots, inventory is full
        Debug.Log("Inventory is full!");
        return false;
    }

    // Empty string = success. Any other string = fail message.
    public string UseItem(InventorySlot slot)
    {
        if (slot.item == null) return "Slot is empty.";

        ItemData item = slot.item;
        PlayerStats player = PlayerStats.instance;

        switch (item.effect)
        {
            case ItemData.ItemEffect.HealHP:
                if (player.currentHealth == player.maxHealth)
                {
                    return "Health is already full!"; // Return the fail message
                }
                player.Heal(item.amount);
                break;

            case ItemData.ItemEffect.HealMP:
                if (player.currentMana == player.maxMana)
                {
                    return "Mana is already full!"; // Return the fail message
                }
                player.RestoreMana(item.amount);
                break;

            case ItemData.ItemEffect.BuffAttack:
                Buff attackBuff = new Buff();
                attackBuff.effect = SkillData.SkillEffect.BuffAttack;
                attackBuff.duration = item.amount;
                player.AddBuff(attackBuff);
                break;

            case ItemData.ItemEffect.Thorns:
                Buff thornsBuff = new Buff();
                thornsBuff.effect = SkillData.SkillEffect.Thorns;
                thornsBuff.duration = item.amount;
                player.AddBuff(thornsBuff);
                break;
        }

        // If we got this far, the item was used.
        RemoveItem(slot, 1);
        return ""; // Return an empty string for SUCCESS
    }

    // Helper function to remove from a specific slot
    public void RemoveItem(InventorySlot slot, int quantity)
    {
        slot.quantity -= quantity;
        if (slot.quantity <= 0)
        {
            slot.item = null;
            slot.quantity = 0;
        }
        OnInventoryChanged?.Invoke(); // Tell the UI to update
    }
}
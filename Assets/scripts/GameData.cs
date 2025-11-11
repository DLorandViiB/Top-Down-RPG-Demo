using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    // --- Player Position ---
    public Vector3 playerPosition;

    // --- Player Stats (from PlayerStats.cs) ---
    public int level;
    public int maxHealth;
    public int currentHealth;
    public int maxMana;
    public int currentMana;
    public int attack;
    public int defense;
    public int luck;
    public int skillPoints;
    public int currentXP;
    public int xpToNextLevel;

    // --- Inventory (from InventoryManager.cs) ---
    public List<string> inventoryItemIDs;
    public List<int> inventoryItemQuantities;

    // --- Skills (from PlayerStats.cs) ---
    public List<string> unlockedSkillIDs;

    // --- Constructor for a New Game ---
    // This defines the default values when starting fresh.
    public GameData()
    {
        // Player Position
        this.playerPosition = new Vector3(2.0f, 5.0f, 0);

        // Player Stats - these should match the defaults in PlayerStats.cs
        this.level = 1;
        this.maxHealth = 100;
        this.currentHealth = 100;
        this.maxMana = 50;
        this.currentMana = 50;
        this.attack = 10;
        this.defense = 5;
        this.luck = 2;
        this.skillPoints = 10;
        this.currentXP = 0;
        this.xpToNextLevel = 100;

        // Inventory
        this.inventoryItemIDs = new List<string>();
        this.inventoryItemQuantities = new List<int>();

        // Skills
        this.unlockedSkillIDs = new List<string>();
    }
}
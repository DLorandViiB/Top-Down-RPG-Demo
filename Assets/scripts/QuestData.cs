using UnityEngine;
using System.Collections.Generic;

public enum QuestType { Fetch, Kill }

[CreateAssetMenu(fileName = "New Quest", menuName = "Quest System/Quest")]
public class QuestData : ScriptableObject
{
    [Header("Info")]
    public string questID;        // Internal ID (e.g. "DungeonKey")
    public string title;          // UI Title (e.g. "The Lost Key")
    [TextArea] public string description; // UI Description

    [Header("Type")]
    public QuestType questType;

    [Header("Requirements")]
    public ItemData itemRequirement; // For Fetch Quests
    public string killTargetID;      // For Kill Quests
    public int requiredAmount = 1;   // How many to kill/collect (Default is 1)

    [Header("Rewards")]
    public List<ItemData> itemRewards;
    public int xpReward;
    public int currencyReward;
}
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Needed for list filtering

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;

    // --- DATA LISTS (The UI reads these) ---
    public List<Quest> activeQuests = new List<Quest>();
    public List<string> completedQuestIDs = new List<string>();

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    // --- ACTIONS: Manage the Lists ---

    public void AcceptQuest(QuestData questData)
    {
        // Don't accept if we already have it or completed it
        if (activeQuests.Any(q => q.data == questData) || completedQuestIDs.Contains(questData.questID))
            return;

        Quest newQuest = new Quest(questData);
        activeQuests.Add(newQuest);

        // Helper: If it's a Kill quest, check if we already killed the target previously?
        // (Optional logic, usually not needed for linear RPGs)
    }

    public void CompleteQuest(QuestData questData)
    {
        // Find the active quest
        Quest q = activeQuests.Find(x => x.data == questData);
        if (q != null)
        {
            activeQuests.Remove(q);
            if (!completedQuestIDs.Contains(questData.questID))
            {
                completedQuestIDs.Add(questData.questID);
            }
        }
    }

    // --- CHECKERS (Used by StoryNPC) ---

    public bool CheckFetchRequirement(ItemData itemNeeded)
    {
        if (InventoryManager.instance == null) return false;
        return InventoryManager.instance.HasItem(itemNeeded);
    }

    public bool CheckKillRequirement(string bossInteractionID)
    {
        if (GameStatemanager.instance == null) return false;

        // 1. Check game memory
        bool isDead = GameStatemanager.instance.IsInteractionCompleted(bossInteractionID);

        // 2. If dead, update the active quest tracker so the UI shows progress
        if (isDead)
        {
            // Find any active kill quest for this boss and update it
            foreach (Quest q in activeQuests)
            {
                if (q.data.questType == QuestType.Kill && q.data.killTargetID == bossInteractionID)
                {
                    q.currentAmount = 1; // Mark as killed in UI
                }
            }
        }

        return isDead;
    }

    public void RemoveQuestItem(ItemData itemToRemove)
    {
        if (itemToRemove.isKeyItem) return; // Safety check

        foreach (var slot in InventoryManager.instance.slots)
        {
            if (slot.item == itemToRemove)
            {
                InventoryManager.instance.RemoveItem(slot, 1);
                return;
            }
        }
    }

    // Returns text for the dialogue box
    public List<string> GrantReward(QuestData quest)
    {
        List<string> rewardMessages = new List<string>();

        // Give XP
        if (quest.xpReward > 0)
        {
            PlayerStats.instance.GainXP(quest.xpReward);
            rewardMessages.Add($"You gained {quest.xpReward} XP!");
        }

        // Give Currency
        if (quest.currencyReward > 0)
        {
            PlayerStats.instance.AddCurrency(quest.currencyReward);
            rewardMessages.Add($"You received {quest.currencyReward} coins!");
        }

        // Give Items
        if (quest.itemRewards != null)
        {
            foreach (ItemData item in quest.itemRewards)
            {
                if (item != null)
                {
                    if (InventoryManager.instance.AddItem(item))
                        rewardMessages.Add($"You received {item.itemName}!");
                    else
                        rewardMessages.Add($"Inventory full! Could not take {item.itemName}.");
                }
            }
        }

        return rewardMessages;
    }
}

// This defines what a "Quest" looks like in the active list
[System.Serializable]
public class Quest
{
    public QuestData data;
    public int currentAmount; // Tracks progress (e.g. 0/1 Bosses killed)

    public Quest(QuestData d)
    {
        this.data = d;
        this.currentAmount = 0;
    }
}
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;

    // --- DRAG ALL YOUR QUESTS HERE IN INSPECTOR ---
    [Header("Configuration")]
    public List<QuestData> allGameQuests = new List<QuestData>();

    // --- RUNTIME DATA ---
    public List<Quest> activeQuests = new List<Quest>();
    public List<string> completedQuestIDs = new List<string>();

    void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    // --- HELPER: LOOKUP QUEST DATA BY ID ---
    public QuestData GetQuestDataByID(string id)
    {
        return allGameQuests.Find(q => q.questID == id);
    }

    // --- ACTIONS ---
    public void AcceptQuest(QuestData questData)
    {
        if (activeQuests.Any(q => q.data == questData) || completedQuestIDs.Contains(questData.questID)) return;
        activeQuests.Add(new Quest(questData));
    }

    public void CompleteQuest(QuestData questData)
    {
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

    // --- CHECKERS ---
    public bool CheckFetchRequirement(ItemData itemNeeded)
    {
        if (InventoryManager.instance == null) return false;
        return InventoryManager.instance.HasItem(itemNeeded);
    }

    public bool CheckKillRequirement(string bossInteractionID)
    {
        if (GameStatemanager.instance == null) return false;

        // Check if boss is dead in GameState
        bool isDead = GameStatemanager.instance.IsInteractionCompleted(bossInteractionID);

        // If dead, update the active quest tracker immediately
        if (isDead)
        {
            foreach (Quest q in activeQuests)
            {
                if (q.data.questType == QuestType.Kill && q.data.killTargetID == bossInteractionID)
                {
                    q.currentAmount = q.data.requiredAmount;
                }
            }
        }
        return isDead;
    }

    public void RemoveQuestItem(ItemData itemToRemove)
    {
        if (itemToRemove.isKeyItem) return;
        foreach (var slot in InventoryManager.instance.slots)
        {
            if (slot.item == itemToRemove) { InventoryManager.instance.RemoveItem(slot, 1); return; }
        }
    }

    public List<string> GrantReward(QuestData quest)
    {
        List<string> rewardMessages = new List<string>();
        if (quest.xpReward > 0)
        {
            PlayerStats.instance.GainXP(quest.xpReward);
            rewardMessages.Add($"You gained {quest.xpReward} XP!");
        }
        if (quest.currencyReward > 0)
        {
            PlayerStats.instance.AddCurrency(quest.currencyReward);
            rewardMessages.Add($"You received {quest.currencyReward} coins!");
        }
        if (quest.itemRewards != null)
        {
            foreach (ItemData item in quest.itemRewards)
            {
                if (item != null && InventoryManager.instance.AddItem(item))
                    rewardMessages.Add($"You received {item.itemName}!");
            }
        }
        return rewardMessages;
    }
}

[System.Serializable]
public class Quest
{
    public QuestData data;
    public int currentAmount;

    public Quest(QuestData d)
    {
        this.data = d;
        this.currentAmount = 0;
    }
}
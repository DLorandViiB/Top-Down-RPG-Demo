using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class StoryNPC : MonoBehaviour, IInteractable
{
    [Header("Setup")]
    public string npcID = "StoryGiver";
    public GameObject indicator;

    [Header("Quests")]
    public QuestData quest1_FindKey;
    public QuestData quest2_KillBoss;

    [Header("Dialogue")]
    [TextArea] public string[] d_Intro;
    [TextArea] public string[] d_DuringKey;
    [TextArea] public string[] d_CompleteKey;
    [TextArea] public string[] d_DuringBoss;
    [TextArea] public string[] d_Victory;

    // Self-Healing: When game loads, ensure the Quest Manager knows what stage we are at
    void Start()
    {
        if (indicator) indicator.SetActive(false);
        SyncQuestLog();
    }

    void SyncQuestLog()
    {
        int stage = GetCurrentStage();
        // If we are in Stage 1, ensure Quest 1 is in the UI log
        if (stage == 1) QuestManager.instance.AcceptQuest(quest1_FindKey);
        // If we are in Stage 2, ensure Quest 1 is complete and Quest 2 is active
        if (stage == 2)
        {
            QuestManager.instance.CompleteQuest(quest1_FindKey);
            QuestManager.instance.AcceptQuest(quest2_KillBoss);
        }
    }

    public void OnInteract()
    {
        AudioManager.instance.PlaySFX("WorldObjects");
        int stage = GetCurrentStage();

        // STAGE 0: START
        if (stage == 0)
        {
            DialogueManager.instance.StartDialogue(d_Intro, () => {
                GameStatemanager.instance.MarkInteractionAsCompleted(npcID + "_Started");
                // ADD TO LOG
                QuestManager.instance.AcceptQuest(quest1_FindKey);
            });
        }
        // STAGE 1: KEY
        else if (stage == 1)
        {
            if (QuestManager.instance.CheckFetchRequirement(quest1_FindKey.itemRequirement))
            {
                DialogueManager.instance.StartDialogue(d_CompleteKey, OnKeyTurnInComplete);
            }
            else
            {
                DialogueManager.instance.StartDialogue(d_DuringKey, null);
            }
        }
        // STAGE 2: BOSS
        else if (stage == 2)
        {
            if (QuestManager.instance.CheckKillRequirement(quest2_KillBoss.killTargetID))
            {
                // BOSS DEFEATED
                // Mark quest as complete in UI right before victory
                QuestManager.instance.CompleteQuest(quest2_KillBoss);

                DialogueManager.instance.StartDialogue(d_Victory, () => {
                    SceneManager.LoadScene("VictoryScene");
                });
            }
            else
            {
                DialogueManager.instance.StartDialogue(d_DuringBoss, null);
            }
        }
    }

    private void OnKeyTurnInComplete()
    {
        // 1. Logic
        QuestManager.instance.RemoveQuestItem(quest1_FindKey.itemRequirement);
        List<string> rewardsText = QuestManager.instance.GrantReward(quest1_FindKey);
        GameStatemanager.instance.MarkInteractionAsCompleted(npcID + "_KeyTurnedIn");

        // 2. Update Log
        QuestManager.instance.CompleteQuest(quest1_FindKey); // Move Q1 to completed
        QuestManager.instance.AcceptQuest(quest2_KillBoss);  // Add Q2 to active

        // 3. UI Feedback
        if (rewardsText.Count > 0)
            DialogueManager.instance.StartDialogue(rewardsText.ToArray(), null);
    }

    private int GetCurrentStage()
    {
        bool hasStarted = GameStatemanager.instance.IsInteractionCompleted(npcID + "_Started");
        bool hasTurnedInKey = GameStatemanager.instance.IsInteractionCompleted(npcID + "_KeyTurnedIn");

        if (!hasStarted) return 0;
        if (!hasTurnedInKey) return 1;
        return 2;
    }

    public void ShowIndicator() { if (indicator) indicator.SetActive(true); }
    public void HideIndicator() { if (indicator) indicator.SetActive(false); }
}
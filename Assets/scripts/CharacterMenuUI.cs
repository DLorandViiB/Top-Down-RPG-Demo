using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

public class CharacterMenuUI : MonoBehaviour
{
    // --- ENUMS ---
    public enum MenuPage { QuestLog, Inventory, SkillTree }

    [Header("Menu Logic")]
    public GameObject characterMenu;
    private bool isMenuOpen = false;
    private PlayerMovement playerMovement;
    private MenuPage currentPage = MenuPage.Inventory;

    [Header("Inventory Panel")]
    public GameObject slotPrefab;
    public Transform inventoryPanel;
    public int columns = 6;
    private List<GameObject> inventorySlots = new List<GameObject>();
    private int selectedSlotIndex = 0;

    [Header("Panel Groups")]
    public RectTransform questLogGroup;
    public RectTransform statsAndInventoryGroup;
    public RectTransform skillTreeGroup;

    [Header("Quest Log UI")]
    public GameObject questSlotPrefab;
    public Transform activeQuestContainer;
    public Transform completedQuestContainer;
    public TextMeshProUGUI questTitleText;
    public TextMeshProUGUI questObjectiveText;
    public TextMeshProUGUI questDescriptionText;

    // Single index for the entire vertical list
    private int questSelectedIndex = 0;
    private List<GameObject> allQuestUIObjects = new List<GameObject>();

    [Header("Skill Tree UI")]
    public TextMeshProUGUI skillPointsText;
    public TextMeshProUGUI skillNameText;
    public TextMeshProUGUI skillDescriptionText;
    public SkillTreeNode startingNode;
    private SkillTreeNode currentNode;

    [Header("Item Description")]
    public TextMeshProUGUI itemDescriptionNameText;
    public TextMeshProUGUI itemDescriptionText;

    [Header("Stats Panel UI")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI defenseText;
    public TextMeshProUGUI luckText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI currencyText;
    public TextMeshProUGUI buffListText;

    [Header("Slide Settings")]
    public float slideSpeed = 8f;
    private Vector2 onScreenPos;
    private Vector2 offScreenPosLeft;
    private Vector2 offScreenPosRight;
    private bool isAnimating = false;

    void Start()
    {
        CreateInventorySlots();

        Canvas rootCanvas = characterMenu.GetComponentInParent<Canvas>();
        onScreenPos = statsAndInventoryGroup.anchoredPosition;
        float slideDistance = rootCanvas.GetComponent<RectTransform>().rect.width * 3f;

        offScreenPosLeft = new Vector2(onScreenPos.x - slideDistance, onScreenPos.y);
        offScreenPosRight = new Vector2(onScreenPos.x + slideDistance, onScreenPos.y);

        // --- INITIAL POSITIONING ---
        statsAndInventoryGroup.anchoredPosition = onScreenPos;
        skillTreeGroup.anchoredPosition = offScreenPosRight;
        if (questLogGroup) questLogGroup.anchoredPosition = offScreenPosLeft;

        if (InventoryManager.instance != null)
            InventoryManager.instance.OnInventoryChanged += RedrawInventory;

        RedrawInventory();
        PlayerStats.instance.OnStatsChanged += UpdateBuffListUI;

        characterMenu.SetActive(false);
        isMenuOpen = false;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.cKey.wasPressedThisFrame) ToggleMenu();
        if (!isMenuOpen || isAnimating) return;

        // --- SLIDING LOGIC ---
        if (keyboard.qKey.wasPressedThisFrame)
        {
            if (currentPage == MenuPage.Inventory)
            {
                StartCoroutine(SlidePanels(statsAndInventoryGroup, questLogGroup, offScreenPosRight, onScreenPos));
                currentPage = MenuPage.QuestLog;
                RedrawQuestLog();
            }
            else if (currentPage == MenuPage.SkillTree)
            {
                StartCoroutine(SlidePanels(skillTreeGroup, statsAndInventoryGroup, offScreenPosRight, onScreenPos));
                currentPage = MenuPage.Inventory;
            }
        }
        else if (keyboard.eKey.wasPressedThisFrame)
        {
            if (currentPage == MenuPage.Inventory)
            {
                StartCoroutine(SlidePanels(statsAndInventoryGroup, skillTreeGroup, offScreenPosLeft, onScreenPos));
                currentPage = MenuPage.SkillTree;
            }
            else if (currentPage == MenuPage.QuestLog)
            {
                StartCoroutine(SlidePanels(questLogGroup, statsAndInventoryGroup, offScreenPosLeft, onScreenPos));
                currentPage = MenuPage.Inventory;
            }
        }

        // --- CONTEXT NAVIGATION ---
        switch (currentPage)
        {
            case MenuPage.Inventory:
                HandleInventoryNavigation();
                break;
            case MenuPage.SkillTree:
                HandleSkillTreeNavigation();
                break;
            case MenuPage.QuestLog:
                HandleQuestNavigation();
                break;
        }

        if (keyboard.escapeKey.wasPressedThisFrame) CloseMenu();
    }

    // ========================================================================
    //                              QUEST LOG LOGIC (UPDATED)
    // ========================================================================

    void RedrawQuestLog()
    {
        if (QuestManager.instance == null) return;

        // 1. Clear old lists and the combined tracker
        foreach (Transform child in activeQuestContainer) Destroy(child.gameObject);
        foreach (Transform child in completedQuestContainer) Destroy(child.gameObject);
        allQuestUIObjects.Clear();

        // 2. Build Active List (Top Panel)
        foreach (var q in QuestManager.instance.activeQuests)
        {
            GameObject go = Instantiate(questSlotPrefab, activeQuestContainer);
            go.GetComponentInChildren<TextMeshProUGUI>().text = q.data.title;

            // Add to our "master list" for navigation
            allQuestUIObjects.Add(go);
        }

        // 3. Build Completed List (Bottom Panel)
        foreach (string id in QuestManager.instance.completedQuestIDs)
        {
            GameObject go = Instantiate(questSlotPrefab, completedQuestContainer);

            // Try to find the quest name from the ID if possible, otherwise show ID
            // (If you don't have a lookup method, this just shows the ID string)
            go.GetComponentInChildren<TextMeshProUGUI>().text = id + " (Done)";

            // Add to our "master list" for navigation
            allQuestUIObjects.Add(go);
        }

        // Reset selection to top
        questSelectedIndex = 0;
        UpdateQuestSelectionUI();
    }

    void HandleQuestNavigation()
    {
        if (allQuestUIObjects.Count == 0) return;

        var keyboard = Keyboard.current;
        int previousIndex = questSelectedIndex;

        // Vertical Navigation only
        if (keyboard.upArrowKey.wasPressedThisFrame)
        {
            questSelectedIndex--;
        }
        else if (keyboard.downArrowKey.wasPressedThisFrame)
        {
            questSelectedIndex++;
        }

        // Clamp the index so it stays within the total list (Active + Completed)
        questSelectedIndex = Mathf.Clamp(questSelectedIndex, 0, allQuestUIObjects.Count - 1);

        if (questSelectedIndex != previousIndex)
        {
            UpdateQuestSelectionUI();
        }
    }

    void UpdateQuestSelectionUI()
    {
        if (allQuestUIObjects.Count == 0)
        {
            questTitleText.text = "No Quests";
            questObjectiveText.text = "";
            questDescriptionText.text = "";
            return;
        }

        // 1. Visual Highlight
        // Turn off ALL highlights first
        foreach (var go in allQuestUIObjects)
        {
            Transform highlight = go.transform.Find("Highlight");
            if (highlight) highlight.gameObject.SetActive(false);
        }

        // Turn on the highlight for the selected one
        GameObject selectedObj = allQuestUIObjects[questSelectedIndex];
        Transform selectedHighlight = selectedObj.transform.Find("Highlight");
        if (selectedHighlight) selectedHighlight.gameObject.SetActive(true);

        // 2. Determine if we are in the "Active" section or "Completed" section
        int activeCount = QuestManager.instance.activeQuests.Count;

        if (questSelectedIndex < activeCount)
        {
            // --- WE ARE SELECTING AN ACTIVE QUEST ---
            Quest q = QuestManager.instance.activeQuests[questSelectedIndex];

            questTitleText.text = q.data.title;
            questDescriptionText.text = q.data.description;

            // Show objective status
            if (q.data.questType == QuestType.Fetch)
            {
                // Ideally, use InventoryManager.instance.GetItemCount(q.data.itemRequirement) here
                questObjectiveText.text = $"Objective: Retrieve {q.data.itemRequirement.itemName}";
            }
            else if (q.data.questType == QuestType.Kill)
            {
                questObjectiveText.text = $"Objective: Kill {q.data.killTargetID} ({q.currentAmount}/{q.data.requiredAmount})";
            }
        }
        else
        {
            // --- WE ARE SELECTING A COMPLETED QUEST ---
            // The index inside the completed list is (questSelectedIndex - activeCount)
            int completedIndex = questSelectedIndex - activeCount;
            string questID = QuestManager.instance.completedQuestIDs[completedIndex];

            questTitleText.text = questID;
            questObjectiveText.text = "<color=green>COMPLETED</color>";
            questDescriptionText.text = "You have successfully finished this quest.";
        }
    }

    // ========================================================================
    //                              STANDARD MENU LOGIC
    // ========================================================================

    void ToggleMenu()
    {
        if (isMenuOpen) CloseMenu();
        else OpenMenu();
    }

    void OpenMenu()
    {
        if (playerMovement == null) playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null && !playerMovement.canMove) return;

        isMenuOpen = true;
        characterMenu.SetActive(true);
        GameStatemanager.instance.RequestUIPause();

        if (PlayerStats.instance != null)
        {
            PlayerStats.instance.OnStatsChanged += UpdateSkillPoints;
            PlayerStats.instance.OnStatsChanged += UpdateStatsText;
            PlayerStats.instance.OnStatsChanged += UpdateBuffListUI;
        }

        currentPage = MenuPage.Inventory;
        statsAndInventoryGroup.anchoredPosition = onScreenPos;
        skillTreeGroup.anchoredPosition = offScreenPosRight;
        if (questLogGroup) questLogGroup.anchoredPosition = offScreenPosLeft;

        UpdateSlotSelection();
        UpdateSkillPoints();
        UpdateStatsText();
        UpdateBuffListUI();
    }

    void CloseMenu()
    {
        isMenuOpen = false;
        characterMenu.SetActive(false);
        GameStatemanager.instance.ReleaseUIPause();

        if (PlayerStats.instance != null)
        {
            PlayerStats.instance.OnStatsChanged -= UpdateSkillPoints;
            PlayerStats.instance.OnStatsChanged -= UpdateStatsText;
            PlayerStats.instance.OnStatsChanged -= UpdateBuffListUI;
        }
    }

    void HandleInventoryNavigation()
    {
        if (inventorySlots.Count == 0) return;
        var keyboard = Keyboard.current;
        int previousIndex = selectedSlotIndex;
        if (keyboard.rightArrowKey.wasPressedThisFrame) selectedSlotIndex++;
        else if (keyboard.leftArrowKey.wasPressedThisFrame) selectedSlotIndex--;
        else if (keyboard.upArrowKey.wasPressedThisFrame) selectedSlotIndex -= columns;
        else if (keyboard.downArrowKey.wasPressedThisFrame) selectedSlotIndex += columns;
        selectedSlotIndex = Mathf.Clamp(selectedSlotIndex, 0, inventorySlots.Count - 1);
        if (selectedSlotIndex != previousIndex) UpdateSlotSelection();

        if (keyboard.zKey.wasPressedThisFrame)
        {
            InventorySlot slot = InventoryManager.instance.slots[selectedSlotIndex];
            if (slot.item != null && slot.item.canUseInMenu) InventoryManager.instance.UseItem(slot);
        }
    }

    void RedrawInventory()
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            InventorySlot dataSlot = InventoryManager.instance.slots[i];
            Image icon = inventorySlots[i].transform.Find("Icon").GetComponent<Image>();
            TextMeshProUGUI quantityText = inventorySlots[i].transform.Find("QuantityText").GetComponent<TextMeshProUGUI>();

            if (dataSlot.item != null)
            {
                icon.gameObject.SetActive(true);
                icon.sprite = dataSlot.item.icon;
                quantityText.text = dataSlot.quantity > 1 ? dataSlot.quantity.ToString() : "";
            }
            else
            {
                icon.gameObject.SetActive(false);
                quantityText.text = "";
            }
        }
    }

    void HandleSkillTreeNavigation()
    {
        var keyboard = Keyboard.current;
        SkillTreeNode nextNode = null;

        if (keyboard.upArrowKey.wasPressedThisFrame) nextNode = currentNode.nodeUp;
        else if (keyboard.downArrowKey.wasPressedThisFrame) nextNode = currentNode.nodeDown;
        else if (keyboard.leftArrowKey.wasPressedThisFrame) nextNode = currentNode.nodeLeft;
        else if (keyboard.rightArrowKey.wasPressedThisFrame) nextNode = currentNode.nodeRight;

        if (nextNode != null)
        {
            currentNode.DeselectNode();
            currentNode = nextNode;
            currentNode.SelectNode();
            UpdateSkillDescription();
        }
        if (keyboard.zKey.wasPressedThisFrame) currentNode.OnNodeClicked();
    }

    public void UpdateSkillPoints()
    {
        if (PlayerStats.instance != null && skillPointsText != null)
            skillPointsText.SetText($"Skill Points: {PlayerStats.instance.skillPoints}");
        RefreshAllNodeVisuals();
    }

    void RefreshAllNodeVisuals()
    {
        if (skillTreeGroup == null) return;
        foreach (SkillTreeNode node in skillTreeGroup.GetComponentsInChildren<SkillTreeNode>())
        {
            if (node.skillData != null && PlayerStats.instance != null) node.UpdateNodeVisuals(PlayerStats.instance);
        }
    }

    void UpdateSlotSelection()
    {
        if (inventorySlots.Count == 0) return;
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            inventorySlots[i].transform.Find("Highlight").gameObject.SetActive(i == selectedSlotIndex);
        }
        InventorySlot selected = InventoryManager.instance.slots[selectedSlotIndex];
        if (selected.item != null)
        {
            itemDescriptionNameText.text = selected.item.itemName;
            itemDescriptionText.text = selected.item.description;
        }
        else
        {
            itemDescriptionNameText.text = "";
            itemDescriptionText.text = "";
        }
    }

    void CreateInventorySlots()
    {
        if (slotPrefab == null || inventoryPanel == null) return;
        foreach (Transform child in inventoryPanel) Destroy(child.gameObject);
        inventorySlots.Clear();
        for (int i = 0; i < 24; i++)
        {
            GameObject slot = Instantiate(slotPrefab, inventoryPanel);
            inventorySlots.Add(slot);
        }
    }

    IEnumerator SlidePanels(RectTransform panelToHide, RectTransform panelToShow, Vector2 hidePos, Vector2 showPos)
    {
        isAnimating = true;
        float t = 0;
        Vector2 startHidePos = panelToHide.anchoredPosition;
        Vector2 startShowPos = panelToShow.anchoredPosition;

        if (panelToShow == skillTreeGroup && startingNode != null) { currentNode = startingNode; currentNode.SelectNode(); UpdateSkillDescription(); }

        while (t < 1.0f)
        {
            t += Time.unscaledDeltaTime * slideSpeed;
            panelToHide.anchoredPosition = Vector2.Lerp(startHidePos, hidePos, t);
            panelToShow.anchoredPosition = Vector2.Lerp(startShowPos, showPos, t);
            yield return null;
        }

        panelToHide.anchoredPosition = hidePos;
        panelToShow.anchoredPosition = showPos;
        isAnimating = false;
    }

    void UpdateSkillDescription()
    {
        if (currentNode == null || currentNode.skillData == null)
        {
            skillNameText.text = "???";
            skillDescriptionText.text = "Select a skill.";
            return;
        }
        skillNameText.text = currentNode.skillData.skillName;
        skillDescriptionText.text = currentNode.skillData.description;
    }

    void UpdateStatsText()
    {
        if (PlayerStats.instance == null) return;
        nameText.text = PlayerStats.instance.playerName;
        levelText.text = "Level: " + PlayerStats.instance.level;
        attackText.text = "Attack: " + PlayerStats.instance.attack;
        defenseText.text = "Defense: " + PlayerStats.instance.defense;
        luckText.text = "Luck: " + PlayerStats.instance.luck;
        currencyText.text = "Coins: " + PlayerStats.instance.currentCurrency;
    }

    void UpdateBuffListUI()
    {
        if (PlayerStats.instance == null || buffListText == null) return;
        buffListText.text = "Active Buffs:\n";
        if (PlayerStats.instance.activeBuffs.Count == 0) { buffListText.text += "None"; return; }
        foreach (Buff buff in PlayerStats.instance.activeBuffs)
        {
            buffListText.text += $"{buff.effect} ({buff.duration})\n";
        }
    }

    void OnDestroy()
    {
        if (InventoryManager.instance != null) InventoryManager.instance.OnInventoryChanged -= RedrawInventory;
        if (PlayerStats.instance != null)
        {
            PlayerStats.instance.OnStatsChanged -= UpdateSkillPoints;
            PlayerStats.instance.OnStatsChanged -= UpdateStatsText;
            PlayerStats.instance.OnStatsChanged -= UpdateBuffListUI;
        }
    }
}
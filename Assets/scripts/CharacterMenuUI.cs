using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Collections; // Needed for Coroutines
using TMPro; // Needed for TextMeshPro

public class CharacterMenuUI : MonoBehaviour
{
    [Header("Menu Logic")]
    public GameObject characterMenu; // The parent GameObject that you show/hide
    private bool isMenuOpen = false;
    private PlayerMovement playerMovement;

    [Header("Inventory Panel")]
    public GameObject slotPrefab;
    public Transform inventoryPanel; // The 'InventoryBackground' grid
    public int columns = 6;
    private List<GameObject> slots = new List<GameObject>();
    private int selectedSlotIndex = 0;

    [Header("Panel Groups & Switching")]
    public RectTransform statsAndInventoryGroup; // Drag your 'StatsAndInventoryGroup' here
    public RectTransform skillTreeGroup;       // Drag your 'SkillTreeGroup' here

    [Header("Skill Tree UI")]
    public TextMeshProUGUI skillPointsText;
    public TextMeshProUGUI skillNameText;
    public TextMeshProUGUI skillDescriptionText;

    [Header("Item Description")]
    public TextMeshProUGUI itemDescriptionNameText;
    public TextMeshProUGUI itemDescriptionText;

    [Header("Slide Settings")]
    public float slideSpeed = 8f;
    private Vector2 onScreenPos;
    private Vector2 offScreenPosLeft;
    private Vector2 offScreenPosRight;

    [Header("Skill Tree Logic")]
    public SkillTreeNode startingNode; // Drag your top-most skill node here
    private SkillTreeNode currentNode;

    private bool isInventoryOnScreen = true; // Start with inventory visible
    private bool isAnimating = false;

    [Header("Stats Panel UI")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI defenseText;
    public TextMeshProUGUI luckText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI buffListText;

    void Start()
    {
        // Find player
        playerMovement = FindAnyObjectByType<PlayerMovement>();

        // Create inventory slots
        CreateSlots();

        // Get the root Canvas to calculate screen width
        Canvas rootCanvas = characterMenu.GetComponentInParent<Canvas>();

        onScreenPos = statsAndInventoryGroup.anchoredPosition; // Should be 0,0
        float slideDistance = rootCanvas.GetComponent<RectTransform>().rect.width * 2.0f;

        offScreenPosLeft = new Vector2(onScreenPos.x - slideDistance, onScreenPos.y);
        offScreenPosRight = new Vector2(onScreenPos.x + slideDistance, onScreenPos.y);

        // Set initial positions (skill tree starts hidden)
        statsAndInventoryGroup.anchoredPosition = onScreenPos;
        skillTreeGroup.anchoredPosition = offScreenPosRight;  

        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.OnInventoryChanged += RedrawInventory;
        }

        RedrawInventory();

        PlayerStats.instance.OnStatsChanged += UpdateBuffListUI;

        // Start with menu closed
        characterMenu.SetActive(false);
        isMenuOpen = false;
        Time.timeScale = 1f; // Ensure time is running
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Toggle the entire menu
        if (keyboard.cKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }

        // If the menu is closed, do nothing else
        if (!isMenuOpen) return;

        // If panels are sliding, do nothing else
        if (isAnimating) return;

        // --- Panel Switching ---
        if (keyboard.eKey.wasPressedThisFrame && isInventoryOnScreen)
        {
            StartCoroutine(SlidePanels(statsAndInventoryGroup, skillTreeGroup, offScreenPosLeft, onScreenPos));
            isInventoryOnScreen = false;
        }
        else if (keyboard.qKey.wasPressedThisFrame && !isInventoryOnScreen)
        {
            StartCoroutine(SlidePanels(skillTreeGroup, statsAndInventoryGroup, offScreenPosRight, onScreenPos));
            isInventoryOnScreen = true;
        }

        // --- Context-Aware Navigation ---
        if (isInventoryOnScreen)
        {
            HandleMenuNavigation(); // Your inventory grid logic
        }
        else
        {
            HandleSkillTreeNavigation(); // Our new skill tree logic
        }

        // Escape key to close
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            CloseMenu();
        }
    }

    void UpdateStatsText()
    {
        if (PlayerStats.instance == null) return;

        // Read from the persistent PlayerStats
        nameText.text = PlayerStats.instance.playerName;
        levelText.text = "Level: " + PlayerStats.instance.level;
        attackText.text = "Attack: " + PlayerStats.instance.attack;
        defenseText.text = "Defense: " + PlayerStats.instance.defense;
        luckText.text = "Luck: " + PlayerStats.instance.luck;
    }

    // --- MENU OPEN/CLOSE FUNCTIONS ---

    void ToggleMenu()
    {
        if (isMenuOpen) { CloseMenu(); }
        else { OpenMenu(); }
    }

    void OpenMenu()
    {
        if (isMenuOpen || characterMenu == null) return;

        isMenuOpen = true;
        characterMenu.SetActive(true);
        Time.timeScale = 0f;

        if (playerMovement != null)
        {
            playerMovement.canMove = false;
            playerMovement.StopMovement();
        }

        if (PlayerStats.instance != null)
        {
            PlayerStats.instance.OnStatsChanged += UpdateSkillPoints;
            PlayerStats.instance.OnStatsChanged += UpdateStatsText;
            PlayerStats.instance.OnStatsChanged += UpdateBuffListUI;
        }

        // Reset to inventory view every time menu opens
        isInventoryOnScreen = true;
        statsAndInventoryGroup.anchoredPosition = onScreenPos;
        skillTreeGroup.anchoredPosition = offScreenPosRight;

        // Update UI
        UpdateSlotSelection();
        UpdateSkillPoints();
        UpdateStatsText();
        UpdateBuffListUI();
    }

    void CloseMenu()
    {
        if (!isMenuOpen || characterMenu == null) return;

        isMenuOpen = false;
        characterMenu.SetActive(false);
        Time.timeScale = 1f;

        if (playerMovement != null)
        {
            playerMovement.canMove = true;
        }

        if (PlayerStats.instance != null)
        {
            PlayerStats.instance.OnStatsChanged -= UpdateSkillPoints;
            PlayerStats.instance.OnStatsChanged -= UpdateStatsText;
            PlayerStats.instance.OnStatsChanged -= UpdateBuffListUI;
        }

        buffListText.text = "";
    }

    // --- NAVIGATION FUNCTIONS ---

    void HandleMenuNavigation()
    {
        if (slots.Count == 0) return;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        int previousIndex = selectedSlotIndex;
        if (keyboard.rightArrowKey.wasPressedThisFrame) selectedSlotIndex++;
        else if (keyboard.leftArrowKey.wasPressedThisFrame) selectedSlotIndex--;
        else if (keyboard.upArrowKey.wasPressedThisFrame) selectedSlotIndex -= columns;
        else if (keyboard.downArrowKey.wasPressedThisFrame) selectedSlotIndex += columns;
        selectedSlotIndex = Mathf.Clamp(selectedSlotIndex, 0, slots.Count - 1);
        if (selectedSlotIndex != previousIndex) { UpdateSlotSelection(); }

        if (keyboard.zKey.wasPressedThisFrame)
        {
            // Get the slot we're on
            InventorySlot slotToUse = InventoryManager.instance.slots[selectedSlotIndex];

            // Check if it's usable in the menu
            if (slotToUse.item != null && slotToUse.item.canUseInMenu)
            {
                // Try to use it
                InventoryManager.instance.UseItem(slotToUse);
                // OnInventoryChanged will auto-redraw the slots
                // OnStatsChanged will auto-update our HP/MP/Buffs
            }
            else
            {
                // Play a "fail" sound
            }
        }
    }

    void RedrawInventory()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            // Get the data for this slot from the "brain" (InventoryManager)
            InventorySlot dataSlot = InventoryManager.instance.slots[i];

            // Get the UI components from our visual slot
            Image icon = slots[i].transform.Find("Icon").GetComponent<Image>();
            TextMeshProUGUI quantityText = slots[i].transform.Find("QuantityText").GetComponent<TextMeshProUGUI>();

            if (dataSlot.item != null)
            {
                // Slot is NOT empty:
                icon.gameObject.SetActive(true);
                icon.sprite = dataSlot.item.icon;

                if (dataSlot.quantity > 1)
                {
                    quantityText.text = dataSlot.quantity.ToString();
                }
                else
                {
                    quantityText.text = "";
                }
            }
            else
            {
                // Slot is empty:
                icon.gameObject.SetActive(false);
                quantityText.text = "";
            }
        }
    }

    void HandleSkillTreeNavigation()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        SkillTreeNode nextNode = null;

        if (keyboard.upArrowKey.wasPressedThisFrame)
        {
            nextNode = currentNode.nodeUp;
        }
        else if (keyboard.downArrowKey.wasPressedThisFrame)
        {
            nextNode = currentNode.nodeDown;
        }
        else if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            nextNode = currentNode.nodeLeft;
        }
        else if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            nextNode = currentNode.nodeRight;
        }

        // If we found a valid next node, move to it
        if (nextNode != null)
        {
            currentNode.DeselectNode();
            currentNode = nextNode;
            currentNode.SelectNode();

            UpdateSkillDescription();
        }

        // Check for "click"
        if (keyboard.zKey.wasPressedThisFrame) // Or your 'Enter'/'Space' key
        {
            currentNode.OnNodeClicked();
        }
    }

    // --- UI UPDATE FUNCTIONS ---

    public void UpdateSkillPoints()
    {
        if (PlayerStats.instance != null && skillPointsText != null)
        {
            skillPointsText.SetText($"Skill Points: {PlayerStats.instance.skillPoints}");
        }
        else if (skillPointsText != null)
        {
            skillPointsText.SetText("Skill Points: --");
        }

        RefreshAllNodeVisuals();
    }

    void RefreshAllNodeVisuals()
    {
        // Check if the panel is active
        if (skillTreeGroup == null) return;

        SkillTreeNode[] allNodes = skillTreeGroup.GetComponentsInChildren<SkillTreeNode>();
        foreach (SkillTreeNode node in allNodes)
        {
            if (node.skillData != null && PlayerStats.instance != null)
            {
                node.UpdateNodeVisuals(PlayerStats.instance);
            }
        }
    }

    void UpdateSlotSelection()
    {
        if (slots.Count == 0) return;

        for (int i = 0; i < slots.Count; i++)
        {
            Transform highlight = slots[i].transform.Find("Highlight");
            if (highlight != null)
                highlight.gameObject.SetActive(i == selectedSlotIndex);
        }

        // Now, update the description box based on the selected slot
        InventorySlot selectedSlotData = InventoryManager.instance.slots[selectedSlotIndex];

        if (selectedSlotData.item != null)
        {
            // We've selected a slot WITH an item
            itemDescriptionNameText.text = selectedSlotData.item.itemName;
            itemDescriptionText.text = selectedSlotData.item.description;
        }
        else
        {
            // We've selected an EMPTY slot
            itemDescriptionNameText.text = "";
            itemDescriptionText.text = "";
        }
    }

    void CreateSlots()
    {
        if (slotPrefab == null || inventoryPanel == null) return;
        foreach (Transform child in inventoryPanel) { Destroy(child.gameObject); }
        slots.Clear();
        for (int i = 0; i < 24; i++)
        {
            GameObject slot = Instantiate(slotPrefab, inventoryPanel);
            slots.Add(slot);
        }
    }

    // --- SLIDING COROUTINE ---

    IEnumerator SlidePanels(RectTransform panelToHide, RectTransform panelToShow, Vector2 hidePos, Vector2 showPos)
    {
        isAnimating = true;
        float t = 0;
        Vector2 startHidePos = panelToHide.anchoredPosition;
        Vector2 startShowPos = panelToShow.anchoredPosition;

        while (t < 1.0f)
        {
            t += Time.unscaledDeltaTime * slideSpeed;
            panelToHide.anchoredPosition = Vector2.Lerp(startHidePos, hidePos, t);
            panelToShow.anchoredPosition = Vector2.Lerp(startShowPos, showPos, t);
            yield return null;
        }

        panelToHide.anchoredPosition = hidePos;
        panelToShow.anchoredPosition = showPos;

        // If we are showing the Skill Tree
        if (panelToShow == skillTreeGroup)
        {
            if (startingNode != null)
            {
                currentNode = startingNode;
                currentNode.SelectNode();

                // Update the description box for the starting node
                UpdateSkillDescription();
            }
        }
        // If we are hiding the Skill Tree
        else if (panelToHide == skillTreeGroup)
        {
            if (currentNode != null)
            {
                currentNode.DeselectNode();
            }
            currentNode = null;

            // Clear the description box
            UpdateSkillDescription();
        }

        isAnimating = false;

    }

    void UpdateSkillDescription()
    {
        if (currentNode == null || currentNode.skillData == null)
        {
            skillNameText.text = "???";
            skillDescriptionText.text = "Select a skill to see details.";
            return;
        }

        skillNameText.text = currentNode.skillData.skillName;
        skillDescriptionText.text = currentNode.skillData.description;
    }

    void UpdateBuffListUI()
    {
        if (PlayerStats.instance == null || buffListText == null) return;

        buffListText.text = "Active Buffs:\n";

        if (PlayerStats.instance.activeBuffs.Count == 0)
        {
            buffListText.text += "None";
            return;
        }

        foreach (Buff buff in PlayerStats.instance.activeBuffs)
        {
            // This will show "BuffAttack (3 Turns)"
            buffListText.text += $"{buff.effect.ToString()} ({buff.duration} Turns)\n";
        }
    }

    void OnDestroy()
    {
        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.OnInventoryChanged -= RedrawInventory;
        }

        if (PlayerStats.instance != null)
        {
            PlayerStats.instance.OnStatsChanged -= UpdateSkillPoints;
            PlayerStats.instance.OnStatsChanged -= UpdateStatsText;
            PlayerStats.instance.OnStatsChanged -= UpdateBuffListUI;
        }
    }
}
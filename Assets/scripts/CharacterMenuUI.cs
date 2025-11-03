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
    public int columns = 5;
    private List<GameObject> slots = new List<GameObject>();
    private int selectedSlotIndex = 0;

    [Header("Panel Groups & Switching")]
    public RectTransform statsAndInventoryGroup; // Drag your 'StatsAndInventoryGroup' here
    public RectTransform skillTreeGroup;       // Drag your 'SkillTreeGroup' here
    public TextMeshProUGUI skillPointsText;    // Drag your 'SkillPointsText' here

    [Header("Slide Settings")]
    public float slideSpeed = 8f;
    private Vector2 onScreenPos;
    private Vector2 offScreenPosLeft;
    private Vector2 offScreenPosRight;

    private bool isInventoryOnScreen = true; // Start with inventory visible
    private bool isAnimating = false;

    void Start()
    {
        Debug.Log("=== CHARACTER MENU CONTROLLER START ===");

        // Find player
        playerMovement = FindAnyObjectByType<PlayerMovement>();

        // Create inventory slots
        CreateSlots();

        // --- NEW SLIDER START LOGIC ---
        // Get the root Canvas to calculate screen width
        Canvas rootCanvas = characterMenu.GetComponentInParent<Canvas>();

        onScreenPos = statsAndInventoryGroup.anchoredPosition; // Should be 0,0
        float slideDistance = rootCanvas.GetComponent<RectTransform>().rect.width * 2.0f;

        offScreenPosLeft = new Vector2(onScreenPos.x - slideDistance, onScreenPos.y);
        offScreenPosRight = new Vector2(onScreenPos.x + slideDistance, onScreenPos.y);

        // Set initial positions (skill tree starts hidden)
        statsAndInventoryGroup.anchoredPosition = onScreenPos;
        skillTreeGroup.anchoredPosition = offScreenPosRight;
        // --- END OF NEW START LOGIC ---

        // Subscribe to PlayerStats event ONE TIME.
        // Since this script is always active, it will always listen.
        if (PlayerStats.instance != null)
        {
            PlayerStats.instance.OnStatsChanged += UpdateSkillPoints;
        }

        // Start with menu closed
        characterMenu.SetActive(false);
        isMenuOpen = false;
        Time.timeScale = 1f; // Ensure time is running

        Debug.Log("Menu Manager Ready - Press C to open");
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

    // --- MENU OPEN/CLOSE FUNCTIONS ---

    void ToggleMenu()
    {
        if (isMenuOpen) { CloseMenu(); }
        else { OpenMenu(); }
    }

    void OpenMenu()
    {
        if (isMenuOpen || characterMenu == null) return;

        Debug.Log("OPENING MENU");
        isMenuOpen = true;
        characterMenu.SetActive(true);
        Time.timeScale = 0f;

        if (playerMovement != null)
        {
            playerMovement.canMove = false;
            playerMovement.StopMovement();
        }

        // Reset to inventory view every time menu opens
        isInventoryOnScreen = true;
        statsAndInventoryGroup.anchoredPosition = onScreenPos;
        skillTreeGroup.anchoredPosition = offScreenPosRight;

        // Update UI
        UpdateSlotSelection();
        UpdateSkillPoints(); // Update points text when menu opens
    }

    void CloseMenu()
    {
        if (!isMenuOpen || characterMenu == null) return;

        Debug.Log("CLOSING MENU");
        isMenuOpen = false;
        characterMenu.SetActive(false);
        Time.timeScale = 1f;

        if (playerMovement != null)
        {
            playerMovement.canMove = true;
        }
    }

    // --- NAVIGATION FUNCTIONS ---

    void HandleMenuNavigation()
    {
        // ... (This is your old inventory navigation, it's perfect)
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
    }

    void HandleSkillTreeNavigation()
    {
        // TODO: Add your skill tree button navigation logic here
        var keyboard = Keyboard.current;
        if (keyboard.upArrowKey.wasPressedThisFrame)
        {
            Debug.Log("Navigating Skill Tree: UP");
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
    }

    void UpdateSlotSelection()
    {
        // ... (This is your old selection logic, it's perfect)
        if (slots.Count == 0) return;
        for (int i = 0; i < slots.Count; i++)
        {
            Transform highlight = slots[i].transform.Find("Highlight");
            if (highlight != null) highlight.gameObject.SetActive(false);
        }
        if (selectedSlotIndex >= 0 && selectedSlotIndex < slots.Count)
        {
            Transform highlight = slots[selectedSlotIndex].transform.Find("Highlight");
            if (highlight != null) highlight.gameObject.SetActive(true);
        }
    }

    void CreateSlots()
    {
        // ... (This is your old create slots logic, it's perfect)
        if (slotPrefab == null || inventoryPanel == null) return;
        foreach (Transform child in inventoryPanel) { Destroy(child.gameObject); }
        slots.Clear();
        for (int i = 0; i < 20; i++)
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

        isAnimating = false;
    }
}
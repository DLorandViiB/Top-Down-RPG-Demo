using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class CharacterMenuUI : MonoBehaviour
{
    [Header("Menu References")]
    public GameObject characterMenu; // Drag CharacterMenu GameObject here
    public GameObject slotPrefab;
    public Transform inventoryPanel;
    public int columns = 5;

    private List<GameObject> slots = new List<GameObject>();
    private int selectedSlotIndex = 0;
    private bool isMenuOpen = false;
    private PlayerMovement playerMovement;

    void Start()
    {
        Debug.Log("=== CHARACTER MENU CONTROLLER START ===");

        playerMovement = FindAnyObjectByType<PlayerMovement>();

        CreateSlots();

        // Start with menu closed
        if (characterMenu != null)
        {
            isMenuOpen = characterMenu.activeSelf;
            if (isMenuOpen)
            {
                CloseMenu();
            }
        }

        Debug.Log("Menu Manager Ready - Press C to open");
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.cKey.wasPressedThisFrame)
        {
            Debug.Log("C KEY PRESSED");
            ToggleMenu();
        }

        if (isMenuOpen)
        {
            HandleMenuNavigation();
        }
    }

    void CreateSlots()
    {
        if (slotPrefab == null || inventoryPanel == null) return;

        foreach (Transform child in inventoryPanel)
        {
            Destroy(child.gameObject);
        }
        slots.Clear();

        for (int i = 0; i < 20; i++)
        {
            GameObject slot = Instantiate(slotPrefab, inventoryPanel);
            slots.Add(slot);
        }
    }

    void ToggleMenu()
    {
        if (isMenuOpen)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    void OpenMenu()
    {
        if (isMenuOpen || characterMenu == null) return;

        Debug.Log("OPENING MENU");
        isMenuOpen = true;
        characterMenu.SetActive(true); // Enable the menu GameObject
        Time.timeScale = 0f;

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        selectedSlotIndex = 0;
        UpdateSlotSelection();
    }

    void CloseMenu()
    {
        if (!isMenuOpen || characterMenu == null) return;

        Debug.Log("CLOSING MENU");
        isMenuOpen = false;
        characterMenu.SetActive(false); // Disable the menu GameObject
        Time.timeScale = 1f;

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
    }

    void HandleMenuNavigation()
    {
        // ... (same navigation code as before)
        if (slots.Count == 0) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        int previousIndex = selectedSlotIndex;

        if (keyboard.rightArrowKey.wasPressedThisFrame)
            selectedSlotIndex++;
        else if (keyboard.leftArrowKey.wasPressedThisFrame)
            selectedSlotIndex--;
        else if (keyboard.upArrowKey.wasPressedThisFrame)
            selectedSlotIndex -= columns;
        else if (keyboard.downArrowKey.wasPressedThisFrame)
            selectedSlotIndex += columns;

        selectedSlotIndex = Mathf.Clamp(selectedSlotIndex, 0, slots.Count - 1);

        if (selectedSlotIndex != previousIndex)
        {
            UpdateSlotSelection();
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            CloseMenu();
        }
    }

    void UpdateSlotSelection()
    {
        // ... (same selection code as before)
        if (slots.Count == 0) return;

        for (int i = 0; i < slots.Count; i++)
        {
            Transform highlight = slots[i].transform.Find("Highlight");
            if (highlight != null)
                highlight.gameObject.SetActive(false);
        }

        if (selectedSlotIndex >= 0 && selectedSlotIndex < slots.Count)
        {
            Transform highlight = slots[selectedSlotIndex].transform.Find("Highlight");
            if (highlight != null)
                highlight.gameObject.SetActive(true);
        }
    }
}
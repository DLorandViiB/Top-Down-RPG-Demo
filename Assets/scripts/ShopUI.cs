using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class ShopUI : MonoBehaviour
{
    public static ShopUI instance;

    [Header("Main Panel")]
    public GameObject shopPanel; // The entire shop parent object
    public TextMeshProUGUI playerCurrencyText;

    [Header("Description Panel (Bottom)")]
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;
    public TextMeshProUGUI itemPriceText;

    [Header("Sliding Panels")]
    public RectTransform buyPanelGroup;  // The "Buy" panel
    public RectTransform sellPanelGroup; // The "Sell" panel

    [Header("Panel Indicators (Tabs)")]
    [Tooltip("Text that says 'Sell (E)->'")]
    public GameObject buyPanelIndicator;
    [Tooltip("Text that says '<- (Q) Buy'")]
    public GameObject sellPanelIndicator;

    [Header("Slot Containers")]
    [Tooltip("The parent object with the VerticalLayoutGroup for BUY slots")]
    public Transform buyListPanel;
    [Tooltip("The prefab for a shop slot")]
    public GameObject slotPrefab;
    [Tooltip("The parent object with the VerticalLayoutGroup for SELL slots")]
    public Transform sellListPanel;

    [Header("Slide Settings (from CharacterMenuUI)")]
    public float slideSpeed = 8f;
    private Vector2 onScreenPos;
    private Vector2 offScreenPosLeft;
    private Vector2 offScreenPosRight;

    // --- STATE ---
    private PlayerMovement playerMovement;
    private bool isShopOpen = false;
    private bool isBuyPanelOnScreen = true; // Start on the "Buy" panel
    private bool isAnimating = false; // To prevent spamming

    // Item lists
    private List<ItemData> currentMerchantStock;
    private List<InventorySlot> playerSellableItems;

    // UI Slot lists
    private List<ShopSlotUI> buySlots = new List<ShopSlotUI>();
    private List<ShopSlotUI> sellSlots = new List<ShopSlotUI>();

    private int selectedBuyIndex = 0;
    private int selectedSellIndex = 0;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }

        playerSellableItems = new List<InventorySlot>();
        buySlots = new List<ShopSlotUI>();
        sellSlots = new List<ShopSlotUI>();
    }

    void Start()
    {
        // We calculate the slide positions.
        onScreenPos = buyPanelGroup.anchoredPosition; // Should be (0, 0)

        // Use Screen.width for a more robust slide distance
        float slideDistance = ((RectTransform)transform).rect.width;

        offScreenPosLeft = new Vector2(onScreenPos.x - slideDistance, onScreenPos.y);
        offScreenPosRight = new Vector2(onScreenPos.x + slideDistance, onScreenPos.y);

        // Set initial positions
        buyPanelGroup.anchoredPosition = onScreenPos;
        sellPanelGroup.anchoredPosition = offScreenPosRight;

        shopPanel.SetActive(false);
    }

    // This is called by the MerchantNPC
    public void OpenShop(List<ItemData> itemsForSale)
    {
        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        // 1. Freeze player and stop time
        if (playerMovement)
        {
            playerMovement.canMove = false;
            playerMovement.StopMovement();
        }
        Time.timeScale = 0f;

        // 2. Set state
        isShopOpen = true;
        shopPanel.SetActive(true);
        this.currentMerchantStock = itemsForSale;

        // 3. Reset to default state
        isBuyPanelOnScreen = true;
        buyPanelGroup.anchoredPosition = onScreenPos;
        sellPanelGroup.anchoredPosition = offScreenPosRight;
        selectedBuyIndex = 0;
        selectedSellIndex = 0;

        // 4. Subscribe to events
        PlayerStats.instance.OnStatsChanged += UpdateCurrencyText;
        InventoryManager.instance.OnInventoryChanged += RedrawSellList;

        // 5. Draw everything
        RedrawBuyList();
        RedrawSellList();
        UpdateCurrencyText();
        UpdatePanelSelection();
    }

    public void CloseShop()
    {
        // 1. Unfreeze
        if (playerMovement)
        {
            playerMovement.canMove = true;
        }
        Time.timeScale = 1f;

        // 2. Reset state
        isShopOpen = false;
        shopPanel.SetActive(false);
        isAnimating = false;

        // 3. Unsubscribe from events
        PlayerStats.instance.OnStatsChanged -= UpdateCurrencyText;
        InventoryManager.instance.OnInventoryChanged -= RedrawSellList;
    }

    void Update()
    {
        if (!isShopOpen || isAnimating) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 1. Close Menu
        if (keyboard.xKey.wasPressedThisFrame)
        {
            CloseShop();
            return;
        }

        // 2. Panel Switching
        if (keyboard.eKey.wasPressedThisFrame && isBuyPanelOnScreen)
        {
            // Switch from Buy to Sell
            StartCoroutine(SlidePanels(buyPanelGroup, sellPanelGroup, offScreenPosLeft, onScreenPos));
            isBuyPanelOnScreen = false;
        }
        else if (keyboard.qKey.wasPressedThisFrame && !isBuyPanelOnScreen)
        {
            // Switch from Sell to Buy
            StartCoroutine(SlidePanels(sellPanelGroup, buyPanelGroup, offScreenPosRight, onScreenPos));
            isBuyPanelOnScreen = true;
        }

        // 3. Handle Navigation
        if (isBuyPanelOnScreen)
        {
            HandleNavigation(keyboard, buySlots, ref selectedBuyIndex);
        }
        else
        {
            HandleNavigation(keyboard, sellSlots, ref selectedSellIndex);
        }

        // 4. Handle "Confirm"
        if (keyboard.zKey.wasPressedThisFrame)
        {
            OnConfirm();
        }
    }

    IEnumerator SlidePanels(RectTransform panelToHide, RectTransform panelToShow, Vector2 hidePos, Vector2 showPos)
    {
        isAnimating = true;
        float t = 0;
        Vector2 startHidePos = panelToHide.anchoredPosition;
        Vector2 startShowPos = panelToShow.anchoredPosition;

        while (t < 1.0f)
        {
            // We use unscaledDeltaTime because Time.timeScale is 0!
            t += Time.unscaledDeltaTime * slideSpeed;
            panelToHide.anchoredPosition = Vector2.Lerp(startHidePos, hidePos, t);
            panelToShow.anchoredPosition = Vector2.Lerp(startShowPos, showPos, t);
            yield return null;
        }

        panelToHide.anchoredPosition = hidePos;
        panelToShow.anchoredPosition = showPos;

        // Update UI after the slide is finished
        UpdatePanelSelection();
        isAnimating = false;
    }

    void HandleNavigation(Keyboard keyboard, List<ShopSlotUI> list, ref int index)
    {
        if (list.Count == 0) return;

        int oldIndex = index;

        if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            index++;
        }
        else if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            index--;
        }

        index = Mathf.Clamp(index, 0, list.Count - 1);

        if (oldIndex != index)
        {
            UpdateSlotSelection();
        }
    }

    void OnConfirm()
    {
        if (isBuyPanelOnScreen)
        {
            // --- TRY TO BUY ---
            if (buySlots.Count == 0 || selectedBuyIndex >= currentMerchantStock.Count) return;

            ItemData itemToBuy = currentMerchantStock[selectedBuyIndex];

            if (PlayerStats.instance.SpendCurrency(itemToBuy.price))
            {
                InventoryManager.instance.AddItem(itemToBuy);
            }
            else
            {
                Debug.Log("Not enough currency!");
            }
        }
        else
        {
            // --- TRY TO SELL ---
            if (sellSlots.Count == 0 || selectedSellIndex >= playerSellableItems.Count) return;

            InventorySlot slotToSell = playerSellableItems[selectedSellIndex];

            int sellPrice = Mathf.Max(1, Mathf.RoundToInt(slotToSell.item.price * 0.5f));

            PlayerStats.instance.AddCurrency(sellPrice);
            InventoryManager.instance.RemoveItem(slotToSell, 1);
        }
    }


    void RedrawBuyList()
    {
        foreach (Transform child in buyListPanel) { Destroy(child.gameObject); }
        buySlots.Clear();

        for (int i = 0; i < currentMerchantStock.Count; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, buyListPanel);
            ShopSlotUI slotUI = slotObj.GetComponent<ShopSlotUI>();
            slotUI.Setup(currentMerchantStock[i]);
            buySlots.Add(slotUI);
        }
        UpdateSlotSelection(); // Update after redrawing
    }

    void RedrawSellList()
    {
        foreach (Transform child in sellListPanel) { Destroy(child.gameObject); }
        sellSlots.Clear();
        playerSellableItems.Clear();

        // Find all sellable items
        foreach (InventorySlot slot in InventoryManager.instance.slots)
        {
            if (slot.item != null)
            {
                playerSellableItems.Add(slot); // Add to our logic list

                GameObject slotObj = Instantiate(slotPrefab, sellListPanel);
                ShopSlotUI slotUI = slotObj.GetComponent<ShopSlotUI>();
                slotUI.Setup(slot); // Setup the visual
                sellSlots.Add(slotUI);
            }
        }
        UpdateSlotSelection(); // Update after redrawing
    }

    void UpdateCurrencyText()
    {
        playerCurrencyText.text = $"Coins: {PlayerStats.instance.currentCurrency}";
    }

    void UpdatePanelSelection()
    {
        // This just toggles the "Buy (E->)" and "<- (Q) Sell" hints
        if (buyPanelIndicator) buyPanelIndicator.SetActive(isBuyPanelOnScreen);
        if (sellPanelIndicator) sellPanelIndicator.SetActive(!isBuyPanelOnScreen);

        // And reset the description box
        UpdateSlotSelection();
    }

    void UpdateSlotSelection()
    {
        // 1. Deselect all slots
        foreach (var slot in buySlots) { slot.Deselect(); }
        foreach (var slot in sellSlots) { slot.Deselect(); }

        // 2. Select the correct one and update description
        if (isBuyPanelOnScreen)
        {
            if (buySlots.Count > 0 && selectedBuyIndex < buySlots.Count)
            {
                buySlots[selectedBuyIndex].Select();
                ItemData item = currentMerchantStock[selectedBuyIndex];
                itemNameText.text = item.itemName;
                itemDescriptionText.text = item.description;
                itemPriceText.text = $"{item.price} Coins";
            }
            else
            {
                // No items to buy
                itemNameText.text = "Sold Out";
                itemDescriptionText.text = "This merchant has nothing left to sell.";
                itemPriceText.text = "";
            }
        }
        else
        {
            if (sellSlots.Count > 0 && selectedSellIndex < sellSlots.Count)
            {
                sellSlots[selectedSellIndex].Select();
                ItemData item = playerSellableItems[selectedSellIndex].item;
                int sellPrice = Mathf.Max(1, Mathf.RoundToInt(item.price * 0.5f));
                itemNameText.text = item.itemName;
                itemDescriptionText.text = item.description;
                itemPriceText.text = $"Sells for: {sellPrice} Coins";
            }
            else
            {
                // No items to sell
                itemNameText.text = "Empty Pockets";
                itemDescriptionText.text = "You have no items to sell.";
                itemPriceText.text = "";
            }
        }
    }
}
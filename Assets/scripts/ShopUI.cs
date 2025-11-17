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
    public GameObject shopPanel;
    public TextMeshProUGUI playerCurrencyText;

    [Header("Description Panel (Bottom)")]
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;
    public TextMeshProUGUI itemPriceText;

    [Header("Sliding Panels")]
    public RectTransform buyPanelGroup;
    public RectTransform sellPanelGroup;

    [Header("Panel Indicators (Tabs)")]
    public GameObject buyPanelIndicator;
    public GameObject sellPanelIndicator;

    [Header("Slot Containers")]
    public Transform buyListPanel;
    public GameObject slotPrefab;
    public Transform sellListPanel;

    [Header("Slide Settings (from CharacterMenuUI)")]
    public float slideSpeed = 8f;
    private Vector2 onScreenPos;
    private Vector2 offScreenPosLeft;
    private Vector2 offScreenPosRight;

    // --- STATE ---
    private PlayerMovement playerMovement;
    private bool isShopOpen = false;
    private bool isBuyPanelOnScreen = true;
    private bool isAnimating = false;

    // --- BUG FIX 1: This is now List<InventorySlot> ---
    private List<InventorySlot> currentMerchantStock;

    // --- BUG FIX 2: This is now List<InventorySlot> ---
    private List<InventorySlot> playerSellableItems;

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

        // Initialize the new list types
        playerSellableItems = new List<InventorySlot>();
        currentMerchantStock = new List<InventorySlot>();
        buySlots = new List<ShopSlotUI>();
        sellSlots = new List<ShopSlotUI>();
    }

    void Start()
    {
        onScreenPos = buyPanelGroup.anchoredPosition;
        float slideDistance = ((RectTransform)transform).rect.width;
        offScreenPosLeft = new Vector2(onScreenPos.x - slideDistance, onScreenPos.y);
        offScreenPosRight = new Vector2(onScreenPos.x + slideDistance, onScreenPos.y);

        buyPanelGroup.anchoredPosition = onScreenPos;
        sellPanelGroup.anchoredPosition = offScreenPosRight;
        shopPanel.SetActive(false);
    }

    public void OpenShop(List<InventorySlot> itemsForSale)
    {
        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        GameStatemanager.instance.RequestUIPause();

        // BLOCK other player systems so they can't respond to input while shop is open
        if (playerMovement != null)
        {
            playerMovement.canMove = false;
        }

        // Disable the PlayerInteraction script so Z won't start a new NPC interaction
        PlayerInteraction pi = FindFirstObjectByType<PlayerInteraction>();
        if (pi != null)
        {
            pi.enabled = false;
        }

        // Disable the CharacterMenuUI script so C (and its Update loops) won't open the character menu
        CharacterMenuUI cm = FindFirstObjectByType<CharacterMenuUI>();
        if (cm != null)
        {
            cm.enabled = false;
        }
        // -------------------------------------------------------------------------------

        isShopOpen = true;
        shopPanel.SetActive(true);
        this.currentMerchantStock = itemsForSale; // This now works

        isBuyPanelOnScreen = true;
        buyPanelGroup.anchoredPosition = onScreenPos;
        sellPanelGroup.anchoredPosition = offScreenPosRight;
        selectedBuyIndex = 0;
        selectedSellIndex = 0;

        PlayerStats.instance.OnStatsChanged += UpdateCurrencyText;
        InventoryManager.instance.OnInventoryChanged += RedrawSellList;

        RedrawBuyList();
        RedrawSellList();
        UpdateCurrencyText();
        UpdatePanelSelection();
    }

    public void CloseShop()
    {
        GameStatemanager.instance.ReleaseUIPause();

        // UNBLOCK other player systems
        if (playerMovement != null)
        {
            playerMovement.canMove = true;
        }

        PlayerInteraction pi = FindFirstObjectByType<PlayerInteraction>();
        if (pi != null)
        {
            pi.enabled = true;
        }

        CharacterMenuUI cm = FindFirstObjectByType<CharacterMenuUI>();
        if (cm != null)
        {
            cm.enabled = true;
        }

        isShopOpen = false;
        shopPanel.SetActive(false);
        isAnimating = false;

        PlayerStats.instance.OnStatsChanged -= UpdateCurrencyText;
        InventoryManager.instance.OnInventoryChanged -= RedrawSellList;
    }


    void Update()
    {
        if (!isShopOpen || isAnimating) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.cKey.wasPressedThisFrame)
        {
            CloseShop();
            return;
        }

        if (keyboard.eKey.wasPressedThisFrame && isBuyPanelOnScreen)
        {
            StartCoroutine(SlidePanels(buyPanelGroup, sellPanelGroup, offScreenPosLeft, onScreenPos));
            isBuyPanelOnScreen = false;
        }
        else if (keyboard.qKey.wasPressedThisFrame && !isBuyPanelOnScreen)
        {
            StartCoroutine(SlidePanels(sellPanelGroup, buyPanelGroup, offScreenPosRight, onScreenPos));
            isBuyPanelOnScreen = true;
        }

        if (isBuyPanelOnScreen)
        {
            HandleNavigation(keyboard, buySlots, ref selectedBuyIndex);
        }
        else
        {
            HandleNavigation(keyboard, sellSlots, ref selectedSellIndex);
        }

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
            t += Time.unscaledDeltaTime * slideSpeed;
            panelToHide.anchoredPosition = Vector2.Lerp(startHidePos, hidePos, t);
            panelToShow.anchoredPosition = Vector2.Lerp(startShowPos, showPos, t);
            yield return null;
        }

        panelToHide.anchoredPosition = hidePos;
        panelToShow.anchoredPosition = showPos;

        UpdatePanelSelection();
        isAnimating = false;
    }

    void HandleNavigation(Keyboard keyboard, List<ShopSlotUI> list, ref int index)
    {
        if (list.Count == 0) return;

        int oldIndex = index;

        // We now use Left and Right arrows for navigation.
        if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            index++;
        }
        else if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            index--;
        }

        // Clamp index to the list size
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

            // This is now an InventorySlot
            InventorySlot slotToBuy = currentMerchantStock[selectedBuyIndex];
            ItemData itemToBuy = slotToBuy.item;

            if (PlayerStats.instance.SpendCurrency(itemToBuy.price))
            {
                InventoryManager.instance.AddItem(itemToBuy);

                // Decrement stock
                slotToBuy.quantity--;

                // Redraw the single slot we just bought from
                buySlots[selectedBuyIndex].Setup(slotToBuy, isBuySlot: true);

                if (slotToBuy.quantity <= 0)
                {
                    // Sold out! Remove it and redraw everything
                    currentMerchantStock.RemoveAt(selectedBuyIndex);
                    RedrawBuyList();
                }
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

            // OnInventoryChanged will trigger RedrawSellList,
            // but we can call it manually to be safe.
            RedrawSellList();
        }
    }

    void RedrawBuyList()
    {
        foreach (Transform child in buyListPanel) { Destroy(child.gameObject); }
        buySlots.Clear();

        // currentMerchantStock is now List<InventorySlot>, this is correct
        for (int i = 0; i < currentMerchantStock.Count; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, buyListPanel);
            ShopSlotUI slotUI = slotObj.GetComponent<ShopSlotUI>();

            // This call is now correct
            slotUI.Setup(currentMerchantStock[i], isBuySlot: true);
            buySlots.Add(slotUI);
        }

        // Clamp index after redrawing
        selectedBuyIndex = Mathf.Clamp(selectedBuyIndex, 0, buySlots.Count - 1);
        UpdateSlotSelection();
    }

    void RedrawSellList()
    {
        foreach (Transform child in sellListPanel) { Destroy(child.gameObject); }
        sellSlots.Clear();
        playerSellableItems.Clear();

        foreach (InventorySlot slot in InventoryManager.instance.slots)
        {
            if (slot.item != null)
            {
                playerSellableItems.Add(slot);

                GameObject slotObj = Instantiate(slotPrefab, sellListPanel);
                ShopSlotUI slotUI = slotObj.GetComponent<ShopSlotUI>();

                // This call is now correct
                slotUI.Setup(slot, isBuySlot: false);
                sellSlots.Add(slotUI);
            }
        }

        // Clamp index after redrawing
        selectedSellIndex = Mathf.Clamp(selectedSellIndex, 0, sellSlots.Count - 1);
        UpdateSlotSelection();
    }

    void UpdateCurrencyText()
    {
        playerCurrencyText.text = $"Coins: {PlayerStats.instance.currentCurrency}";
    }

    void UpdatePanelSelection()
    {
        if (buyPanelIndicator) buyPanelIndicator.SetActive(isBuyPanelOnScreen);
        if (sellPanelIndicator) sellPanelIndicator.SetActive(!isBuyPanelOnScreen);

        UpdateSlotSelection();
    }

    void UpdateSlotSelection()
    {
        foreach (var slot in buySlots) { slot.Deselect(); }
        foreach (var slot in sellSlots) { slot.Deselect(); }

        if (isBuyPanelOnScreen)
        {
            if (buySlots.Count > 0 && selectedBuyIndex < buySlots.Count)
            {
                buySlots[selectedBuyIndex].Select();
                InventorySlot slot = currentMerchantStock[selectedBuyIndex];
                itemNameText.text = slot.item.itemName;
                itemDescriptionText.text = slot.item.description;
                itemPriceText.text = $"{slot.item.price} Coins";
            }
            else
            {
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
                itemNameText.text = "Empty Pockets";
                itemDescriptionText.text = "You have no items to sell.";
                itemPriceText.text = "";
            }
        }
    }
}
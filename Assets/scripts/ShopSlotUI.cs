using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image icon;
    public TextMeshProUGUI quantityText;
    public GameObject highlight;

    /// <summary>
    /// A single function to set up the slot for
    /// both Buying (from merchant) and Selling (from player).
    /// </summary>
    public void Setup(InventorySlot slot, bool isBuySlot)
    {
        icon.sprite = slot.item.icon;

        if (isBuySlot)
        {
            // This is a "BUY" slot. Show stock quantity.
            if (quantityText)
            {
                quantityText.gameObject.SetActive(true);
                quantityText.text = $"x{slot.quantity}";
            }
        }
        else
        {
            // This is a "SELL" slot. Show player quantity.
            if (quantityText)
            {
                quantityText.gameObject.SetActive(true);
                quantityText.text = $"x{slot.quantity}";
            }
        }

        Deselect();
    }

    public void Select()
    {
        if (highlight) highlight.SetActive(true);
    }

    public void Deselect()
    {
        if (highlight) highlight.SetActive(false);
    }
}
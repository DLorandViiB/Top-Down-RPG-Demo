using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image icon;
    public TextMeshProUGUI quantityText;
    public GameObject highlight;

    // This is the "Buy" setup (from ItemData)
    public void Setup(ItemData item)
    {
        icon.sprite = item.icon;

        // Hide quantity for "Buy" (since it's infinite)
        if (quantityText) quantityText.gameObject.SetActive(false);
        Deselect();
    }

    // This is the "Sell" setup (from InventorySlot)
    public void Setup(InventorySlot slot)
    {
        icon.sprite = slot.item.icon;

        // Show quantity for "Sell"
        if (quantityText)
        {
            quantityText.gameObject.SetActive(true);
            quantityText.text = $"x{slot.quantity}";
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
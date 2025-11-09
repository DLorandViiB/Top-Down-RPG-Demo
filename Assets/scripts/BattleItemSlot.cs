using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleItemSlot : MonoBehaviour
{
    public TextMeshProUGUI quantityText;
    public GameObject highlight;

    public ItemData itemData;
    public InventorySlot inventorySlot;

    public void Setup(InventorySlot slot)
    {
        inventorySlot = slot;
        itemData = slot.item;

        // We only set the quantity text here.
        // BattleManager now handles the icon.
        if (slot.quantity > 1)
        {
            quantityText.text = slot.quantity.ToString();
        }
        else
        {
            quantityText.text = "";
        }

        highlight.SetActive(false);
    }

    public void Select()
    {
        highlight.SetActive(true);
    }

    public void Deselect()
    {
        highlight.SetActive(false);
    }
}
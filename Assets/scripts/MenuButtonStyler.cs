using UnityEngine;
using UnityEngine.UI;
using TMPro; // Needed for TextMeshPro
using UnityEngine.EventSystems; // Needed for Selection events

public class MenuButtonStyler : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    private TextMeshProUGUI text;
    private Material normalMat;

    [Header("Settings")]
    public float outlineThickness = 0.2f; // Adjust this to make it thicker/thinner
    public Color outlineColor = Color.white;

    void Awake()
    {
        text = GetComponentInChildren<TextMeshProUGUI>();

        // Safety check
        if (text == null)
        {
            Debug.LogError("No TextMeshPro component found on button: " + gameObject.name);
            return;
        }

        // Create a unique material instance so we don't change ALL text at once
        if (text.fontMaterial != null)
        {
            text.fontMaterial = new Material(text.fontMaterial);

            // Set the outline color immediately
            text.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);

            // Start with 0 width (no outline)
            text.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
        }
    }

    // Called when the keyboard selects this button
    public void OnSelect(BaseEventData eventData)
    {
        if (text != null)
        {
            text.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineThickness);

            // Optional: You can also pop the text scale a bit for extra juice
            // transform.localScale = Vector3.one * 1.1f; 
        }
    }

    // Called when the keyboard moves AWAY from this button
    public void OnDeselect(BaseEventData eventData)
    {
        if (text != null)
        {
            text.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);

            // Reset scale if you used the pop effect
            // transform.localScale = Vector3.one;
        }
    }
}
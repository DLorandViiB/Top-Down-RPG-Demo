using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A simple helper script that lives in the MainWorldScene.
/// Its only job is to hold references to scene-specific UI
/// so the persistent GameStatemanager can find them easily.
/// </summary>
public class SceneUIRefs : MonoBehaviour
{
    [Header("Transition UI")]
    public Image fadeScreen;
    public TextMeshProUGUI encounterText;
}
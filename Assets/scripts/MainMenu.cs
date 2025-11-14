using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class MainMenu : MonoBehaviour
{
    [Header("Buttons")]
    public Button newGameButton;
    public Button continueButton;

    [Header("Scene To Load")]
    [SerializeField]
    private string mainGameScene;

    // We need a reference to the manager
    private GameStatemanager gameManager;

    void Start()
    {
        // Find the persistent GameStatemanager in the scene
        gameManager = GameStatemanager.instance;

        if (gameManager == null)
        {
            Debug.LogError("MainMenu ERROR: Could not find GameStatemanager.instance! Is it in your persistent scene?");
            return;
        }

        // --- The Core Logic ---
        // Check if a save file exists
        if (gameManager.DoesSaveFileExist())
        {
            // If yes, enable the 'Continue' button
            continueButton.interactable = true;
        }
        else
        {
            // If no, disable it
            continueButton.interactable = false;
        }
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Check if "Z" was pressed
        if (keyboard.zKey.wasPressedThisFrame)
        {
            // Find out what object is currently selected
            GameObject selectedObject = EventSystem.current.currentSelectedGameObject;

            // If nothing is selected, do nothing
            if (selectedObject == null) return;

            // Try to get a Button component from the selected object
            Button selectedButton = selectedObject.GetComponent<Button>();

            // If it's a valid, interactable button, "click" it
            if (selectedButton != null && selectedButton.interactable)
            {
                selectedButton.onClick.Invoke();
            }
        }
    }

    // --- Button Functions ---
    // These will be hooked up in the Inspector

    public void OnNewGameClicked()
    {
        // Tell the manager to set up a new game
        gameManager.StartNewGame();
    }

    public void OnContinueClicked()
    {
        // Tell the manager to load the saved data
        gameManager.ContinueGame();
    }

    public void OnQuitClicked()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}
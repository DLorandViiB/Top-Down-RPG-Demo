using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField]
    private Button continueButton;

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

    // --- Button Functions ---
    // These will be hooked up in the Inspector

    public void OnNewGameClicked()
    {
        // Tell the manager to set up a new game
        gameManager.StartNewGame();

        // Load the main game scene
        SceneManager.LoadScene(mainGameScene);
    }

    public void OnContinueClicked()
    {
        // Tell the manager to load the saved data
        gameManager.ContinueGame();

        // Load the main game scene
        SceneManager.LoadScene(mainGameScene);
    }

    public void OnQuitClicked()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}
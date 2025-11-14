using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class DeathMenu : MonoBehaviour
{
    [Header("Buttons")]
    public Button continueButton;
    public Button quitButton;

    private GameStatemanager gameManager;

    void Start()
    {
        gameManager = GameStatemanager.instance;

        // Check if a save file exists. If not, "Continue" is impossible.
        if (gameManager == null || !gameManager.DoesSaveFileExist())
        {
            continueButton.interactable = false;
            // Select the Quit button instead
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(quitButton.gameObject);
        }
        else
        {
            // Select the "Continue" button by default
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        }
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Listen for "Z" to click the selected button
        if (keyboard.zKey.wasPressedThisFrame)
        {
            GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
            if (selectedObject == null) return;

            Button selectedButton = selectedObject.GetComponent<Button>();
            if (selectedButton != null && selectedButton.interactable)
            {
                selectedButton.onClick.Invoke();
            }
        }
    }

    // --- Button Functions ---

    public void OnContinueClicked()
    {
        if (gameManager)
        {
            gameManager.ContinueGame();
        }
    }

    public void OnQuitClicked()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}
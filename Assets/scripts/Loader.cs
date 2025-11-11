using UnityEngine;
using UnityEngine.SceneManagement;

public class Loader : MonoBehaviour
{
    public string mainMenuSceneName = "MainMenuScene";

    void Start()
    {
        // Now, we can safely load the main menu.
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
using UnityEngine;
using UnityEngine.SceneManagement; // For scene loading
using System.Collections;       // For coroutines (like fades)
using UnityEngine.UI;           // For the Image
using TMPro;                    // For the Text

public class GameStatemanager : MonoBehaviour
{
    public static GameStatemanager instance; // Singleton

    [Header("Persistent Data")]
    public EnemyData enemyToBattle; // We set this before loading BattleScene

    [Header("Transition UI")]
    public Image fadeScreen;
    public TextMeshProUGUI encounterText;
    public float fadeSpeed = 1.5f;

    private PlayerMovement playerMovement; // To freeze the player
    private Rigidbody2D playerRb;

    [Header("Encounter Cooldown")]
    public float encounterCooldownTime = 5.0f;
    public bool isEncounterOnCooldown = false;

    private SpriteRenderer playerSpriteRenderer;
    public string overworldSceneName = "MainWorldScene";

    void Awake()
    {
        // This is the singleton pattern to make it persistent
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    // Call this function to start the battle transition
    public void StartBattle(EnemyData enemy)
    {
        // Find the player's movement script and Rigidbody
        if (playerMovement == null)
        {
            // Get the movement script from our persistent player
            playerMovement = PlayerStats.instance.GetComponentInChildren<PlayerMovement>();

            // Get the Rigidbody from our persistent player
            playerRb = PlayerStats.instance.GetComponentInChildren<Rigidbody2D>();
        }

        // --- ADD A NULL CHECK ---
        if (playerMovement == null)
        {
            Debug.LogError("GameStatemanager could not find the 'PlayerMovement' script!");
            return; // Stop here if we can't find it
        }

        StartCoroutine(BattleTransition(enemy));
    }

    private IEnumerator BattleTransition(EnemyData enemy)
    {
        // 1. Store enemy and freeze player
        enemyToBattle = enemy;
        playerMovement.enabled = false;
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
        }

        // 2. Show "Enemy approaching!" text
        encounterText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.5f); // Wait 1.5s
        encounterText.gameObject.SetActive(false);

        // 3. Fade to black
        yield return StartCoroutine(FadeOut());

        // 4. Load the battle scene
        SceneManager.LoadScene("BattleScene"); // Make sure your scene is named this!

        // 5. Fade back in (the BattleManager will call this)
        StartCoroutine(FadeIn());
    }

    public void EndBattle()
    {
        StartCoroutine(EndBattleTransition());
    }

    private IEnumerator EndBattleTransition()
    {
        // 1. Fade to black
        yield return StartCoroutine(FadeOut());

        // 2. Load the main world
        yield return SceneManager.LoadSceneAsync(overworldSceneName);

        // 3. Find all components
        playerMovement = PlayerStats.instance.GetComponentInChildren<PlayerMovement>();
        playerSpriteRenderer = PlayerStats.instance.GetComponentInChildren<SpriteRenderer>();
        playerRb = PlayerStats.instance.GetComponentInChildren<Rigidbody2D>();

        // Stop any current physical movement
        if (playerRb != null) playerRb.linearVelocity = Vector2.zero;

        // Force Unity to clear all "stuck" key presses from its memory
        Input.ResetInputAxes();

        // Make the player visible, but KEEP MOVEMENT DISABLED
        if (playerSpriteRenderer != null) playerSpriteRenderer.enabled = true;
        if (playerMovement != null) playerMovement.enabled = false;

        // 5. Re-link the camera
        CameraFollow camFollow = FindFirstObjectByType<CameraFollow>();
        if (camFollow != null)
        {
            camFollow.target = PlayerStats.instance.transform;
        }
        else
        {
            Debug.LogWarning("GameStatemanager: Could not find CameraFollow script!");
        }

        // 6. Re-enable all encounter zones
        EncounterZone[] zones = FindObjectsByType<EncounterZone>(FindObjectsSortMode.None);
        foreach (EncounterZone zone in zones)
        {
            zone.enabled = true;
        }

        // 7. Start the "safe" cooldown
        StartCoroutine(EncounterCooldown());

        // 8. Fade back in
        yield return StartCoroutine(FadeIn());

        // 9. AFTER fade-in, re-enable the movement script.
        if (playerMovement != null) playerMovement.enabled = true;
    }

    private IEnumerator EncounterCooldown()
    {
        isEncounterOnCooldown = true;
        yield return new WaitForSeconds(encounterCooldownTime);
        isEncounterOnCooldown = false;
    }

    // Coroutine to fade the screen to black
    public IEnumerator FadeOut()
    {
        fadeScreen.color = new Color(0, 0, 0, 0); // Ensure it starts clear
        float alpha = 0;
        while (alpha < 1)
        {
            alpha += Time.deltaTime * fadeSpeed;
            fadeScreen.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeScreen.color = new Color(0, 0, 0, 1); // Ensure it's fully black
    }

    // Coroutine to fade the screen from black
    public IEnumerator FadeIn()
    {
        fadeScreen.color = new Color(0, 0, 0, 1); // Ensure it starts black
        float alpha = 1;
        while (alpha > 0)
        {
            alpha -= Time.deltaTime * fadeSpeed;
            fadeScreen.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeScreen.color = new Color(0, 0, 0, 0); // Ensure it's fully clear
    }
}
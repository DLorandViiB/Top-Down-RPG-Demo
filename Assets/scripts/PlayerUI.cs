using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    // We'll find this automatically, so it's private
    private PlayerStats player;

    [Header("UI References")]
    public Image healthFill;
    public Image manaFill;
    public Image xpFill;

    [Header("Text Labels")]
    public TMP_Text healthText;
    public TMP_Text manaText;
    public TMP_Text xpText;

    [Header("Smoothing")]
    [Range(0.1f, 20f)] public float smoothSpeed = 10f;

    // Target values (from 0 to 1)
    private float hTarget, mTarget, xTarget;
    // Current values (for smoothing)
    private float hCurrent, mCurrent, xCurrent;

    // We use OnEnable, which runs EVERY time the scene loads
    void OnEnable()
    {
        // 1. Find the persistent player
        player = PlayerStats.instance;

        if (player == null)
        {
            Debug.LogError("PlayerUI: Could not find PlayerStats.instance!");
            StartCoroutine(RetryFindPlayer()); // Try again in a frame
            return;
        }

        // 2. Subscribe to the event
        player.OnStatsChanged += SetTargets;

        // 3. Set initial values
        InitializeValues();
    }

    // Always unsubscribe when the object is disabled or scene changes
    void OnDisable()
    {
        if (player != null)
        {
            player.OnStatsChanged -= SetTargets;
        }
    }

    // Failsafe coroutine for race conditions
    private System.Collections.IEnumerator RetryFindPlayer()
    {
        yield return null; // Wait one frame
        player = PlayerStats.instance;
        if (player != null)
        {
            player.OnStatsChanged += SetTargets;
            InitializeValues();
        }
    }

    // Sets the *initial* state of the bars when the UI is first enabled
    void InitializeValues()
    {
        // Set targets
        SetTargets();

        // Set current values immediately so bars are correct on load
        hCurrent = hTarget;
        mCurrent = mTarget;
        xCurrent = xTarget;

        // Update bars and text instantly
        UpdateBars();
        UpdateText();
    }

    // This function is called by the OnStatsChanged event
    // Its ONLY job is to set the new "goal" for the bars
    void SetTargets()
    {
        if (player == null) return;

        hTarget = Mathf.Clamp01((float)player.currentHealth / player.maxHealth);
        mTarget = Mathf.Clamp01((float)player.currentMana / player.maxMana);
        xTarget = player.xpToNextLevel > 0 ? Mathf.Clamp01((float)player.currentXP / player.xpToNextLevel) : 0f;
    }

    // Update() is now ONLY responsible for the smoothing animation
    void Update()
    {
        if (player == null) return; // Don't do anything if we have no player

        // Smooth the "current" values towards the "target" values
        hCurrent = Mathf.MoveTowards(hCurrent, hTarget, smoothSpeed * Time.deltaTime);
        mCurrent = Mathf.MoveTowards(mCurrent, mTarget, smoothSpeed * Time.deltaTime);
        xCurrent = Mathf.MoveTowards(xCurrent, xTarget, smoothSpeed * Time.deltaTime);

        // Update the visual bars
        UpdateBars();

        // Update the text
        UpdateText();
    }

    // Helper function to update the fill amounts
    void UpdateBars()
    {
        if (healthFill) healthFill.fillAmount = hCurrent;
        if (manaFill) manaFill.fillAmount = mCurrent;
        if (xpFill) xpFill.fillAmount = xCurrent;
    }

    // Helper function to update the text labels
    void UpdateText()
    {
        if (healthText)
            healthText.text = $"{player.currentHealth} / {player.maxHealth}";
        if (manaText)
            manaText.text = $"{player.currentMana} / {player.maxMana}";
        if (xpText)
            xpText.text = $"XP: {player.currentXP} / {player.xpToNextLevel}";
    }
}
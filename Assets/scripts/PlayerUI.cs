using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    public PlayerStats player;
    public Image healthFill;
    public Image manaFill;
    public Image xpFill;

    [Header("Text Labels")]
    public TMP_Text healthText;
    public TMP_Text manaText;
    public TMP_Text xpText;

    [Range(0f, 20f)] public float smoothSpeed = 10f;
    private float hTarget, mTarget, xTarget;
    private float hCurrent, mCurrent, xCurrent;

    private void Start()
    {
        if (player == null)
        {
            Debug.LogWarning("Player not assigned in PlayerUI.");
            return;
        }

        hCurrent = hTarget = (float)player.currentHealth / player.maxHealth;
        mCurrent = mTarget = (float)player.currentMana / player.maxMana;
        xCurrent = xTarget = (float)player.currentXP / player.xpToNextLevel;
    }

    private void Update()
    {
        if (player == null) return;

        hTarget = Mathf.Clamp01((float)player.currentHealth / player.maxHealth);
        mTarget = Mathf.Clamp01((float)player.currentMana / player.maxMana);
        xTarget = player.xpToNextLevel > 0 ? Mathf.Clamp01((float)player.currentXP / player.xpToNextLevel) : 0f;

        hCurrent = Mathf.MoveTowards(hCurrent, hTarget, smoothSpeed * Time.deltaTime);
        mCurrent = Mathf.MoveTowards(mCurrent, mTarget, smoothSpeed * Time.deltaTime);
        xCurrent = Mathf.MoveTowards(xCurrent, xTarget, smoothSpeed * Time.deltaTime);

        if (healthFill) healthFill.fillAmount = hCurrent;
        if (manaFill) manaFill.fillAmount = mCurrent;
        if (xpFill) xpFill.fillAmount = xCurrent;

        if (healthText)
            healthText.text = $"{player.currentHealth} / {player.maxHealth}";
        if (manaText)
            manaText.text = $"{player.currentMana} / {player.maxMana}";
        if (xpText)
            xpText.text = $"XP: {player.currentXP} / {player.xpToNextLevel}";
    }
}

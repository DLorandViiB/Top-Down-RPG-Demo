using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public PlayerStats player;
    public Image healthFill;
    public Image manaFill;
    public Image xpFill;

    [Range(0f, 20f)] public float smoothSpeed = 10f;
    private float hTarget, mTarget, xTarget;
    private float hCurrent, mCurrent, xCurrent;

    private void Start()
    {
        if (player == null) Debug.LogWarning("Player not assigned in PlayerUI.");
        hCurrent = hTarget = player != null ? (float)player.currentHealth / player.maxHealth : 1f;
        mCurrent = mTarget = player != null ? (float)player.currentMana / player.maxMana : 1f;
        xCurrent = xTarget = player != null ? (float)player.currentXP / player.xpToNextLevel : 0f;
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
    }
}

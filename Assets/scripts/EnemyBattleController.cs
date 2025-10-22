using UnityEngine;
using UnityEngine.UI; // We'll need this soon
using TMPro; // We'll need this soon

public class EnemyBattleController : MonoBehaviour
{
    // These will be set by the new Setup function
    public EnemyData enemyData;
    public int currentHealth;

    // We can add a reference to the SpriteRenderer here
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        // Get the SpriteRenderer component on this GameObject
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // --- THIS IS THE NEW SETUP FUNCTION ---
    public void Setup(EnemyData data)
    {
        if (data == null)
        {
            // This is our failsafe for testing
            Debug.LogWarning("No EnemyData provided! Using placeholder stats.");
            enemyData = ScriptableObject.CreateInstance<EnemyData>();
            enemyData.enemyName = "Placeholder";
            enemyData.maxHealth = 20;
            // You can keep your red circle sprite as the default
        }
        else
        {
            // This is the normal path!
            enemyData = data;
            spriteRenderer.sprite = enemyData.sprite; // <-- SETS THE SPRITE!
        }

        // Set the stats
        currentHealth = enemyData.maxHealth;
    }

    // (Your TakeDamage function is still good)
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;
    }
}
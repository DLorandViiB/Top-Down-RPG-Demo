using UnityEngine;

public class EnemyBattleController : MonoBehaviour
{
    public string enemyName = "Placeholder Enemy";
    public int maxHealth = 50;
    public int currentHealth;

    public void Setup(string name, int health)
    {
        enemyName = name;
        maxHealth = health;
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }
    }
}
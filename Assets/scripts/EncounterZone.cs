using UnityEngine;

public class EncounterZone : MonoBehaviour
{
    [Header("Encounter Settings")]
    public EnemyData[] possibleEnemies; // Drag your EnemyData assets here

    [Range(0, 1)]
    public float encounterChance = 0.1f; // Chance per second while moving

    private bool playerIsInside = false;

    // We will get this from the player when they enter
    private Rigidbody2D playerRb;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsInside = true;
            // Get the player's Rigidbody to check if they are moving
            playerRb = other.GetComponent<Rigidbody2D>();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsInside = false;
            playerRb = null;
        }
    }

    private void Update()
    {
        // If the player is in the zone AND we have their Rigidbody
        if (playerIsInside && playerRb != null)
        {
            // Check if the player is moving
            bool isMoving = playerRb.linearVelocity.magnitude > 0.1f;

            if (isMoving)
            {
                // Roll the dice (scaled by time)
                if (true)
                {
                    // Found a battle!
                    StartEncounter();
                }
            }
        }
    }

    private void StartEncounter()
    {
        // Stop checking for more battles
        playerIsInside = false; 
        
        // This prevents multiple battles from triggering at once
        this.enabled = false; 

        // Pick a random enemy from our list
        int randomIndex = Random.Range(0, possibleEnemies.Length);
    EnemyData enemyToBattle = possibleEnemies[randomIndex];

    // Tell the (now existing) GameStatemanager to start the battle
    GameStatemanager.instance.StartBattle(enemyToBattle);
        
        // We'll re-enable this script when the battle ends
    }

    // We need a way to re-enable this script after a battle
    // We'll add this later. For now, it will only trigger one battle.
}
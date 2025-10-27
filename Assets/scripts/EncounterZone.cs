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
                if (!GameStatemanager.instance.isEncounterOnCooldown)
                {
                    if (/*Random.value < encounterChance * Time.deltaTime*/ true)
                    {
                        StartEncounter();
                    }
                }
            }
        }
    }

    private void StartEncounter()
    {
        playerIsInside = false;

        // --- THIS LINE IS IMPORTANT ---
        // This stops this specific zone from re-triggering
        this.enabled = false;

        int randomIndex = Random.Range(0, possibleEnemies.Length);
        EnemyData enemyToBattle = possibleEnemies[randomIndex];

        GameStatemanager.instance.StartBattle(enemyToBattle);
    }
}
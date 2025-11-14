using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
    [Tooltip("A unique ID for this spawn point, e.g., 'DungeonEntrance' or 'TownEntrance'")]
    public string spawnPointID;

    // This is just a helper so we can see it in the Scene view
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.5f); // Green
        Gizmos.DrawSphere(transform.position, 0.5f);
        Gizmos.DrawIcon(transform.position + Vector3.up, "PlayerSpawnIcon.png", true);
    }
}
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;     // The player
    public float smoothSpeed = 5f;   // Camera follow speed
    public Vector2 deadZone = new Vector2(1f, 1f); // Width/height of the dead zone

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position;
        Vector3 cameraPos = transform.position;

        // Calculate the offset between camera and player
        Vector3 offset = targetPos - cameraPos;

        // Check if the player is outside the dead zone
        if (Mathf.Abs(offset.x) > deadZone.x / 2f || Mathf.Abs(offset.y) > deadZone.y / 2f)
        {
            // Smoothly move camera toward target
            Vector3 desiredPos = new Vector3(targetPos.x, targetPos.y, cameraPos.z);
            transform.position = Vector3.Lerp(cameraPos, desiredPos, smoothSpeed * Time.deltaTime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw dead zone in Scene view
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(deadZone.x, deadZone.y, 0));
    }
}

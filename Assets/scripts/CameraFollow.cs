using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;
    public Vector2 deadZone = new Vector2(1f, 1f);

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position;
        Vector3 cameraPos = transform.position;

        Vector3 offset = targetPos - cameraPos;

        if (Mathf.Abs(offset.x) > deadZone.x / 2f || Mathf.Abs(offset.y) > deadZone.y / 2f)
        {
            Vector3 desiredPos = new Vector3(targetPos.x, targetPos.y, cameraPos.z);
            transform.position = Vector3.Lerp(cameraPos, desiredPos, smoothSpeed * Time.deltaTime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(deadZone.x, deadZone.y, 0));
    }
}

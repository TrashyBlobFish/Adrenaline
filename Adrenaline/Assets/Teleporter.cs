using UnityEngine;

public class Teleporter : MonoBehaviour
{
    [SerializeField] private Transform destinationA;
    [SerializeField] private Transform destinationB;
    [SerializeField] private float teleportCooldown = 3f;
    [SerializeField] private bool showDebugView = true;
    
    private Collider triggerCollider;
    private static float lastTeleportTime;

    void Start()
    {
        triggerCollider = GetComponent<Collider>();
        
        if (triggerCollider == null)
        {
            Debug.LogError("Teleporter requires a Collider component set as a trigger!");
            return;
        }
        
        if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning("Teleporter collider is not set as a trigger. Enabling trigger mode.");
            triggerCollider.isTrigger = true;
        }
        
        if (destinationA == null || destinationB == null)
        {
            Debug.LogError("Teleporter requires both destination references to be assigned!");
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebugView)
            return;

        if (destinationA != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(destinationA.position, destinationA.position + destinationA.forward * 2f);
        }

        if (destinationB != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(destinationB.position, destinationB.position + destinationB.forward * 2f);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && CanTeleport())
        {
            TeleportPlayer(other.transform);
            lastTeleportTime = Time.time;
        }
    }

    private bool CanTeleport()
    {
        return Time.time - lastTeleportTime >= teleportCooldown;
    }

    private void TeleportPlayer(Transform playerTransform)
    {
        Transform destination = GetOppositeDestination(playerTransform.position);
        
        if (destination != null)
        {
            playerTransform.position = destination.position;
            playerTransform.rotation = destination.rotation;
        }
    }

    private Transform GetOppositeDestination(Vector3 playerPosition)
    {
        float distanceToA = Vector3.Distance(playerPosition, destinationA.position);
        float distanceToB = Vector3.Distance(playerPosition, destinationB.position);
        
        return distanceToA < distanceToB ? destinationB : destinationA;
    }
}

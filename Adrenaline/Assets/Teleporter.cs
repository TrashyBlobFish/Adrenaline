using UnityEngine;
using Unity.Cinemachine;

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
            // Store original rotation to calculate rotation delta
            Quaternion originalRotation = playerTransform.rotation;
            
            // Move and orient player
            playerTransform.position = destination.position;
            playerTransform.rotation = destination.rotation;

            // Rotate player velocity based on rotation change
            Rigidbody playerRigidbody = playerTransform.GetComponent<Rigidbody>();
            if (playerRigidbody != null)
            {
                // Calculate rotation delta
                Quaternion rotationDelta = destination.rotation * Quaternion.Inverse(originalRotation);
                
                // Apply rotation to velocity
                playerRigidbody.linearVelocity = rotationDelta * playerRigidbody.linearVelocity;
            }

            // After teleport, align Cinemachine orbital follow horizontal axis with player facing
            AlignCameraOrbitalAxisWithPlayer(playerTransform);
        }
    }

    private Transform GetOppositeDestination(Vector3 playerPosition)
    {
        float distanceToA = Vector3.Distance(playerPosition, destinationA.position);
        float distanceToB = Vector3.Distance(playerPosition, destinationB.position);
        
        return distanceToA < distanceToB ? destinationB : destinationA;
    }

    /// <summary>
    /// Finds the CameraModeSwitcher on the player, then its third-person Cinemachine camera,
    /// then the CinemachineOrbitalFollow, and sets its horizontal axis so the camera is behind the player.
    /// </summary>
    /// <param name="playerTransform">The teleported player transform.</param>
    private void AlignCameraOrbitalAxisWithPlayer(Transform playerTransform)
    {
        // Grab CameraModeSwitcher on player
        CameraModeSwitcher cameraModeSwitcher = playerTransform.GetComponentInChildren<CameraModeSwitcher>();
        if (cameraModeSwitcher == null)
        {
            Debug.LogWarning("Teleporter: CameraModeSwitcher not found on player.");
            return;
        }

        // Get the third-person CinemachineCamera from CameraModeSwitcher
        CinemachineCamera thirdPersonCam = cameraModeSwitcher.thirdPersonVCam;
        if (thirdPersonCam == null)
        {
            Debug.LogWarning("Teleporter: third-person CinemachineCamera not assigned on CameraModeSwitcher.");
            return;
        }

        // Find CinemachineOrbitalFollow on that camera GameObject
        CinemachineOrbitalFollow orbitalFollow = thirdPersonCam.GetComponent<CinemachineOrbitalFollow>();
        if (orbitalFollow == null)
        {
            Debug.LogWarning("Teleporter: CinemachineOrbitalFollow not found on third-person camera.");
            return;
        }

        // Compute desired yaw so camera is behind the player.
        // Player forward in world space:
        Vector3 forward = playerTransform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
            return;

        forward.Normalize();

        // Convert forward to an angle around Y (yaw). Unity's Y rotation is in degrees.
        float playerYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;

        // Set orbital follow horizontal axis to this yaw
        var horizontalAxis = orbitalFollow.HorizontalAxis;
        horizontalAxis.Value = playerYaw;
        orbitalFollow.HorizontalAxis = horizontalAxis;
    }
}

using UnityEngine;

public class SpeedPitch : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private AudioSource audioSource;

    [Header("Pitch Settings")]
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.5f;
    [SerializeField] private float maxSpeed = 20f;

    void Start()
    {
        // Get the audio source on this GameObject
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogWarning("SpeedPitch: No AudioSource found on this GameObject!");
        }

        // Find the player GameObject by tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement == null)
            {
                Debug.LogWarning("SpeedPitch: PlayerMovement script not found on Player!");
            }
        }
        else
        {
            Debug.LogWarning("SpeedPitch: No GameObject with tag 'Player' found!");
        }
    }

    void Update()
    {
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement == null)
            {
                Debug.LogWarning("SpeedPitch: PlayerMovement script not found on Player!");
            }
        }
        else
        {
            Debug.LogWarning("SpeedPitch: No GameObject with tag 'Player' found!");
        }
        if (playerMovement != null && audioSource != null)
        {
            // Get the player's current speed (assuming PlayerMovement has a way to access speed)
            float currentSpeed = playerMovement.GetComponent<Rigidbody>().linearVelocity.magnitude;

            // Normalize speed and map to pitch range
            float normalizedSpeed = Mathf.Clamp01(currentSpeed / maxSpeed);
            float targetPitch = Mathf.Lerp(minPitch, maxPitch, normalizedSpeed);

            // Smoothly transition to target pitch
            audioSource.pitch = Mathf.Lerp(audioSource.pitch, targetPitch, Time.deltaTime * 5f);
        }
    }
}

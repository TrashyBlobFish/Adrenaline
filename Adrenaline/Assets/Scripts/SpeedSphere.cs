using UnityEngine;
using System.Collections;

public class SpeedSphere : MonoBehaviour
{
    [SerializeField] float speedGained;
    [SerializeField] float speedDuration;
    [SerializeField] float respawnDelay = 15f;
    [SerializeField] string playerAudioSourceName = "SpeedOrb sounds"; // Name of the child object with AudioSource

    private Renderer sphereRenderer;
    private Collider sphereCollider;

    private void Awake()
    {
        sphereRenderer = GetComponent<Renderer>();
        sphereCollider = GetComponent<Collider>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            PlayerMovement playerMove = collision.gameObject.GetComponent<PlayerMovement>();

            if (playerMove != null)
            {
                // Find the audio source by child name
                Transform audioChild = FindChildRecursive(playerMove.transform, playerAudioSourceName);
                AudioSource audioSource = null;

                if (audioChild != null)
                {
                    audioSource = audioChild.GetComponent<AudioSource>();
                }

                // Play pickup sound
                if (audioSource != null)
                {
                    audioSource.Play();
                }

                StartCoroutine(ApplySpeedBoost(playerMove));
            }

            // Hide and disable pickup after use
            sphereRenderer.enabled = false;
            sphereCollider.enabled = false;
            StartCoroutine(RespawnCoroutine());
        }
    }

    private IEnumerator ApplySpeedBoost(PlayerMovement playerMove)
    {
        float originalRunSpeed = playerMove.runSpeed;
        float originalSprintSpeed = playerMove.sprintSpeed;
        playerMove.runSpeed += speedGained;
        playerMove.sprintSpeed += speedGained;

        // Apply green effect to player mesh
        Renderer playerRenderer = playerMove.GetComponentInChildren<MeshRenderer>();
        Color originalColor = Color.white;

        if (playerRenderer != null && playerRenderer.material != null)
        {
            originalColor = playerRenderer.material.color;
            playerRenderer.material.color = Color.green;
        }

        // Wait for duration minus flicker time
        float flickerStartTime = speedDuration - 2f;
        if (flickerStartTime > 0)
        {
            yield return new WaitForSeconds(flickerStartTime);
        }

        // Smooth flicker for last 2 seconds
        float flickerDuration = Mathf.Min(2f, speedDuration);
        float flickerElapsed = 0f;

        while (flickerElapsed < flickerDuration)
        {
            flickerElapsed += Time.deltaTime;
            float normalizedTime = flickerElapsed / flickerDuration;

            // Create smooth sine wave flicker that increases in frequency
            float flickerFrequency = Mathf.Lerp(2f, 8f, normalizedTime);
            float flickerValue = (Mathf.Sin(flickerElapsed * flickerFrequency * Mathf.PI) + 1f) / 2f;

            if (playerRenderer != null && playerRenderer.material != null)
            {
                playerRenderer.material.color = Color.Lerp(originalColor, Color.green, Mathf.Lerp(1f, 0f, normalizedTime) * flickerValue);
            }

            yield return null;
        }

        // Restore original color completely
        if (playerRenderer != null && playerRenderer.material != null)
        {
            playerRenderer.material.color = originalColor;
        }

        // Revert speed boost
        playerMove.runSpeed -= speedGained;
        playerMove.sprintSpeed -= speedGained;
    }

    private IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        sphereRenderer.enabled = true;
        sphereCollider.enabled = true;
    }
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform result = FindChildRecursive(child, childName);
            if (result != null)
                return result;
        }
        return null;
    }
}
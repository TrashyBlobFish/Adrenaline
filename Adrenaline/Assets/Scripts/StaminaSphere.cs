using UnityEngine;
using System.Collections;
using Unity.Netcode;
using System.Linq;

public class StaminaSphere : NetworkBehaviour
{
    [SerializeField] float StaminaRegained;
    [SerializeField] float respawnDelay = 15f;
    [SerializeField] private ParticleSystem explosionParticles;
    [SerializeField] private ParticleSystem internalParticles;

    private Light sphereLight;
    private Renderer sphereRenderer;
    private Collider sphereCollider;
    private Transform targetPlayer;

    private void Awake()
    {
        sphereLight = GetComponent<Light>();
        sphereRenderer = GetComponent<Renderer>();
        sphereCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.GetComponentInChildren<PlayerMovement>().cameraSwitcher.isThirdPerson)
            {
                internalParticles.Stop();
                internalParticles.Clear();
                PlayerMovement movement = other.GetComponentInChildren<PlayerMovement>();
                if (movement == null)
                    return;

                NetworkObject playerNetworkObject = movement.GetComponentInParent<NetworkObject>();
                if (playerNetworkObject == null)
                    return;

                if (!playerNetworkObject.IsOwner)
                    return;

                movement.currentStamina += StaminaRegained;

                if (explosionParticles != null)
                {
                    explosionParticles.transform.parent = null;
                    explosionParticles.Play();
                    targetPlayer = other.transform;
                    StartCoroutine(WaitAndAbsorbParticles());
                }

                CollectServerRpc();
            }
        }
    }

    private IEnumerator WaitAndAbsorbParticles()
    {
        // Wait a frame to let particles spawn
        yield return null;
        // Optionally, wait longer if needed
        yield return new WaitForSeconds(.5f);
        yield return StartCoroutine(AbsorbParticlesCoroutine());
    }

    private IEnumerator AbsorbParticlesCoroutine()
    {
        int maxParticles = explosionParticles.main.maxParticles;
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[maxParticles];
        int count = explosionParticles.GetParticles(particles);
        Debug.Log("Particle count: " + count);

        Vector3[] initialPositions = new Vector3[count];
        float[] durations = new float[count];
        float[] elapsedTimes = new float[count];
        bool[] fading = new bool[count];
        float[] fadeTimes = new float[count];
        float fadeDuration = 0.3f; // How long to fade after contact

        // Set up random durations for each particle
        float minDuration = 0.2f;
        float maxDuration = 1.0f;
        for (int i = 0; i < count; i++)
        {
            initialPositions[i] = particles[i].position;
            durations[i] = Random.Range(minDuration, maxDuration);
            elapsedTimes[i] = 0f;
            fading[i] = false;
            fadeTimes[i] = 0f;
        }

        // Get player renderer and original color
        Renderer playerRenderer = targetPlayer.GetComponentInChildren<MeshRenderer>();
        Color originalColor = playerRenderer.material.color;
        int yellowSteps = 0;
        float tintAmountPerStep = 1f / Mathf.Max(1, count);

        int finishedCount = 0;
        Vector3 targetPosition = targetPlayer.position + Vector3.up * 0.25f;
        if (explosionParticles.main.simulationSpace == ParticleSystemSimulationSpace.Local)
        {
            targetPosition = explosionParticles.transform.InverseTransformPoint(targetPosition);
        }

        while (finishedCount < count)
        {
            // Update target position in case player moves
            Vector3 currentTargetPosition = targetPlayer.position + Vector3.up * 0.25f;
            if (explosionParticles.main.simulationSpace == ParticleSystemSimulationSpace.Local)
            {
                currentTargetPosition = explosionParticles.transform.InverseTransformPoint(currentTargetPosition);
            }

            for (int i = 0; i < count; i++)
            {
                if (fading[i])
                {
                    // Fade out after contact
                    fadeTimes[i] += Time.deltaTime;
                    float fadeT = Mathf.Clamp01(fadeTimes[i] / fadeDuration);
                    particles[i].startColor = new Color32(
                        particles[i].startColor.r,
                        particles[i].startColor.g,
                        particles[i].startColor.b,
                        (byte)Mathf.Lerp(particles[i].startColor.a, 0, fadeT)
                    );
                    if (fadeT >= 1f)
                    {
                        fading[i] = false; // Mark as finished
                        finishedCount++;
                    }
                    continue;
                }

                // Move particle toward player
                elapsedTimes[i] += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTimes[i] / durations[i]);
                particles[i].position = Vector3.Lerp(initialPositions[i], currentTargetPosition, t);

                // Check if close enough to start fading
                if (Vector3.Distance(particles[i].position, currentTargetPosition) < 0.05f)
                {
                    fading[i] = true;
                    fadeTimes[i] = 0f;
                    yellowSteps++;
                    StartCoroutine(TintPlayerYellowCoroutine(playerRenderer, originalColor, yellowSteps, tintAmountPerStep, 2f));
                }
            }

            explosionParticles.SetParticles(particles, count);
            yield return null;
        }

        explosionParticles.Clear();
    }
    private IEnumerator TintPlayerYellowCoroutine(Renderer playerRenderer, Color originalColor, int steps, float tintAmountPerStep, float revertDuration)
    {
        Color yellow = Color.yellow;
        Color targetColor = Color.Lerp(originalColor, yellow, tintAmountPerStep * steps);
        playerRenderer.material.color = targetColor;

        float elapsed = 0f;
        while (elapsed < revertDuration)
        {
            elapsed += Time.deltaTime;
            playerRenderer.material.color = Color.Lerp(targetColor, originalColor, elapsed / revertDuration);
            yield return null;
        }
        playerRenderer.material.color = originalColor;
    }

    private IEnumerator RespawnCoroutine()
    {
        sphereLight.enabled = false;
        sphereRenderer.enabled = false;
        sphereCollider.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        // Reattach explosion if needed
        if (explosionParticles != null && explosionParticles.transform.parent == null)
        {
            explosionParticles.transform.SetParent(transform);
            explosionParticles.transform.localPosition = Vector3.zero;
        }
        sphereLight.enabled = true;
        sphereRenderer.enabled = true;
        sphereCollider.enabled = true;
        internalParticles.Play();
    }

    [ServerRpc(RequireOwnership = false)]
    private void CollectServerRpc()
    {
        StartCoroutine(RespawnCoroutine());
    }
}

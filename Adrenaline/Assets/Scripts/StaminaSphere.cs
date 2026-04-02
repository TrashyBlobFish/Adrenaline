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
    [SerializeField] private Material fullscreenShaderMaterial;

    private Light sphereLight;
    private Renderer sphereRenderer;
    private Collider sphereCollider;
    private Transform targetPlayer;
    private Color originalPlayerColor = Color.white;
    private Material fullscreenShaderInstance;

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
            PlayerMovement movement = other.GetComponentInChildren<PlayerMovement>();
            if (movement == null)
                return;

            NetworkObject playerNetworkObject = movement.GetComponentInParent<NetworkObject>();
            if (playerNetworkObject == null)
                return;

            if (!playerNetworkObject.IsOwner)
                return;

            movement.currentStamina += StaminaRegained;

            if (movement.cameraSwitcher.isThirdPerson)
            {
                internalParticles.Stop();
                internalParticles.Clear();

                if (explosionParticles != null)
                {
                    explosionParticles.transform.parent = null;
                    explosionParticles.Play();
                    targetPlayer = other.transform;
                    StartCoroutine(WaitAndAbsorbParticles());
                }
            }
            else
            {
                // First-person mode: apply fullscreen shader effect
                StartCoroutine(ApplyFirstPersonShaderEffectCoroutine(movement.playerCamera));
            }

            CollectServerRpc();
        }
    }

    private IEnumerator ApplyFirstPersonShaderEffectCoroutine(Camera playerCamera)
    {
        if (fullscreenShaderMaterial == null)
        {
            Debug.LogWarning("Fullscreen shader material not assigned to StaminaSphere");
            yield break;
        }

        // Create an instance of the material to avoid modifying the shared material
        fullscreenShaderInstance = new Material(fullscreenShaderMaterial);

        const float intensityStart = 30f;
        const float intensityEnd = 1.25f;
        const float transitionDuration = 0.25f;
        const float holdDuration = 1f;
        const float reverseDuration = 0.25f;

        // Phase 1: Intensity transition from 30 to 1.25 over 0.25 seconds
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float intensity = Mathf.Lerp(intensityStart, intensityEnd, t);

            fullscreenShaderInstance.SetFloat("_Intensity", intensity);
            fullscreenShaderInstance.SetColor("_Color", Color.yellow);

            yield return null;
        }

        // Ensure we end at exact value
        fullscreenShaderInstance.SetFloat("_Intensity", intensityEnd);

        // Phase 2: Hold the effect for 1 second
        yield return new WaitForSeconds(holdDuration);

        // Phase 3: Reverse the effect (1.25 back to 0) over 0.25 seconds
        elapsed = 0f;
        while (elapsed < reverseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / reverseDuration);
            float intensity = Mathf.Lerp(intensityEnd, 0f, t);

            fullscreenShaderInstance.SetFloat("_Intensity", intensity);
            fullscreenShaderInstance.SetColor("_Color", Color.yellow);

            yield return null;
        }

        // Clean up the instance material
        Destroy(fullscreenShaderInstance);
        fullscreenShaderInstance = null;
    }

    private IEnumerator WaitAndAbsorbParticles()
    {
        yield return null;
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
        float fadeDuration = 0.3f;

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

        Renderer playerRenderer = targetPlayer.GetComponentInChildren<SkinnedMeshRenderer>();
        if (playerRenderer == null)
            yield break;

        // Capture the original material color before tinting
        MaterialPropertyBlock originalBlock = new MaterialPropertyBlock();
        playerRenderer.GetPropertyBlock(originalBlock);
        originalPlayerColor = originalBlock.GetColor("_BaseColor");
        if (originalPlayerColor == Color.black)
        {
            originalPlayerColor = originalBlock.GetColor("_Color");
        }
        if (originalPlayerColor == Color.black)
        {
            originalPlayerColor = Color.white;
        }

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
            Vector3 currentTargetPosition = targetPlayer.position + Vector3.up * 0.25f;
            if (explosionParticles.main.simulationSpace == ParticleSystemSimulationSpace.Local)
            {
                currentTargetPosition = explosionParticles.transform.InverseTransformPoint(currentTargetPosition);
            }

            for (int i = 0; i < count; i++)
            {
                if (fading[i])
                {
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
                        fading[i] = false;
                        finishedCount++;
                    }
                    continue;
                }

                elapsedTimes[i] += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTimes[i] / durations[i]);
                particles[i].position = Vector3.Lerp(initialPositions[i], currentTargetPosition, t);

                if (Vector3.Distance(particles[i].position, currentTargetPosition) < 0.05f)
                {
                    fading[i] = true;
                    fadeTimes[i] = 0f;
                    yellowSteps++;
                    StartCoroutine(TintPlayerYellowCoroutine(playerRenderer, yellowSteps, tintAmountPerStep, 2f));
                }
            }

            explosionParticles.SetParticles(particles, count);
            yield return null;
        }

        explosionParticles.Clear();
    }

    private IEnumerator TintPlayerYellowCoroutine(Renderer playerRenderer, int steps, float tintAmountPerStep, float revertDuration)
    {
        if (playerRenderer == null)
            yield break;

        // Use MaterialPropertyBlock to bypass material overrides and communicate directly with GPU
        MaterialPropertyBlock materialBlock = new MaterialPropertyBlock();

        Color targetTint = Color.Lerp(originalPlayerColor, Color.yellow, tintAmountPerStep * steps);

        Debug.Log($"Tinting player with MaterialPropertyBlock. Target color: {targetTint}");

        // Apply tint via MaterialPropertyBlock
        materialBlock.SetColor("_Color", targetTint);
        materialBlock.SetColor("_BaseColor", targetTint);
        materialBlock.SetColor("_Tint", targetTint);
        playerRenderer.SetPropertyBlock(materialBlock);

        float elapsed = 0f;
        while (elapsed < revertDuration)
        {
            elapsed += Time.deltaTime;
            Color lerpedTint = Color.Lerp(targetTint, originalPlayerColor, elapsed / revertDuration);

            materialBlock.SetColor("_Color", lerpedTint);
            materialBlock.SetColor("_BaseColor", lerpedTint);
            materialBlock.SetColor("_Tint", lerpedTint);
            playerRenderer.SetPropertyBlock(materialBlock);

            yield return null;
        }

        // Clear the property block to restore defaults
        playerRenderer.SetPropertyBlock(null);
    }

    private IEnumerator RespawnCoroutine()
    {
        sphereLight.enabled = false;
        sphereRenderer.enabled = false;
        sphereCollider.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

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
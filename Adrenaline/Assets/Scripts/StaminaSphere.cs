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

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player") == true)
        {
            if (collision.collider.GetComponentInChildren<PlayerMovement>().cameraSwitcher.isThirdPerson)
            {
                internalParticles.Stop();
                internalParticles.Clear();
                PlayerMovement movement = collision.collider.GetComponentInChildren<PlayerMovement>();
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
                    targetPlayer = collision.collider.transform;
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
        float absorbDuration = 0.1f; // Increased for slower fade
        float elapsed = 0f;

        int count = explosionParticles.GetParticles(particles);
        Debug.Log("Particle count: " + count);

        Vector3[] initialPositions = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            initialPositions[i] = particles[i].position;
        }

        while (elapsed < absorbDuration && count > 0)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / absorbDuration);

            Vector3 targetPosition = targetPlayer.position + Vector3.up * 1.5f;
            if (explosionParticles.main.simulationSpace == ParticleSystemSimulationSpace.Local)
            {
                targetPosition = explosionParticles.transform.InverseTransformPoint(targetPosition);
            }

            float fadeT = Mathf.Pow(t, 5f); // Ease out fade

            for (int i = 0; i < count; i++)
            {
                particles[i].position = Vector3.Lerp(initialPositions[i], targetPosition, t);
                particles[i].startColor = new Color32(
                    particles[i].startColor.r,
                    particles[i].startColor.g,
                    particles[i].startColor.b,
                    (byte)Mathf.Lerp(particles[i].startColor.a, 0, fadeT)
                );
            }

            explosionParticles.SetParticles(particles, count);
            yield return null;
        }

        explosionParticles.Clear();
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

using UnityEngine;

public class Glass : MonoBehaviour
{
    public ParticleSystem hitParticles;
    public AudioSource hitAudioSource;
    public float rebuildTime = 30f;

    private BoxCollider objCollider;
    private MeshRenderer meshRenderer;
    private bool isDestroyed = false;

    void Awake()
    {
        // Cache components for performance
        objCollider = GetComponent<BoxCollider>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (hitAudioSource == null)
        {
            hitAudioSource = GetComponent<AudioSource>();
        }

        // Ensure particle system is not playing at start
        if (hitParticles != null)
            hitParticles.Stop();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Shield") && !isDestroyed)
        {
            HandleShieldHit(collision.transform);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Shield") && !isDestroyed)
        {
            HandleShieldHit(other.transform);
        }
    }

    private void HandleShieldHit(Transform contactTransform)
    {
        isDestroyed = true;

        // Rotate particle system shape to face the contact object's forward direction
        if (hitParticles != null)
        {
            var shape = hitParticles.shape;
            Vector3 rotation = Quaternion.LookRotation(contactTransform.forward).eulerAngles;
            rotation.y += -90f;
            shape.rotation = rotation;
        }

        // Play audio effect
        if (hitAudioSource != null)
        {
            hitAudioSource.Play();
        }

        // Disable mesh and collider
        if (objCollider != null)
            objCollider.enabled = false;

        if (meshRenderer != null)
            meshRenderer.enabled = false;

        // Play particle effect
        if (hitParticles != null)
        {
            hitParticles.Play();
        }

        // Schedule rebuild after specified time
        Invoke(nameof(RebuildGlass), rebuildTime);
    }

    public void RebuildGlass()
    {
        isDestroyed = false;

        // Re-enable mesh renderer
        if (meshRenderer != null)
            meshRenderer.enabled = true;

        // Re-enable collider
        if (objCollider != null)
            objCollider.enabled = true;

        // Stop particle effect
        if (hitParticles != null)
            hitParticles.Stop();
    }
}

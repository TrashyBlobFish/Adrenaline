using UnityEngine;

public class Glass : MonoBehaviour
{
    [Header("Assign your particle system here")]
    public ParticleSystem hitParticles;

    private BoxCollider objCollider;
    private MeshRenderer meshRenderer;

    void Awake()
    {
        // Cache components for performance
        objCollider = GetComponent<BoxCollider>();
        meshRenderer = GetComponent<MeshRenderer>();

        // Ensure particle system is not playing at start
        if (hitParticles != null)
            hitParticles.Stop();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Shield"))
        {
            HandleShieldHit();
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Shield"))
        {
            HandleShieldHit();
        }
    }

    private void HandleShieldHit()
    {
        // Remove mesh and collider
        if (objCollider != null)
            Destroy(objCollider);

        if (meshRenderer != null)
            Destroy(meshRenderer);

        // Play particle effect
        if (hitParticles != null)
        {
            hitParticles.Play();
            // Schedule destroy after particle lifetime
            Destroy(gameObject, hitParticles.main.duration);
        }
        else
        {
            // No particle assigned? Just destroy immediately
            Destroy(gameObject);
        }
    }
}

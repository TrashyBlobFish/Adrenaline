using UnityEngine;
using System.Collections;

public class StaminaSphere : MonoBehaviour
{
    [SerializeField] float StaminaRegained;
    [SerializeField] float respawnDelay = 15f;

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
            collision.gameObject.GetComponent<PlayerMovement>().currentStamina += StaminaRegained;
            StartCoroutine(RespawnCoroutine());
        }
    }

    private IEnumerator RespawnCoroutine()
    {
        sphereRenderer.enabled = false;
        sphereCollider.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        sphereRenderer.enabled = true;
        sphereCollider.enabled = true;
    }
}

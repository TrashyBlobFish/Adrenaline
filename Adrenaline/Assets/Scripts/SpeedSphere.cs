using UnityEngine;
using System.Collections;

public class SpeedSphere : MonoBehaviour
{
    [SerializeField] float speedGained;
    [SerializeField] float speedDuration;
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
            PlayerMovement playerMove = collision.gameObject.GetComponent<PlayerMovement>();

            if (playerMove != null)
            {
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

        yield return new WaitForSeconds(speedDuration);

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
}

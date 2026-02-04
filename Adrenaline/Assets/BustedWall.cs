using UnityEngine;
using System.Collections;

public class BustedWall : MonoBehaviour
{
    [SerializeField] private GameObject bustedWallPrefab;
    [SerializeField] private GameObject FullWallPrefab;
    [SerializeField] private AudioSource wallAudioSource;
    [SerializeField] private AudioClip breakClip;

    private GameObject currentBustedWallInstance;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.gameObject.CompareTag("Shield"))
        {
            if (currentBustedWallInstance == null)
            {
                // Disable collider and full wall
                gameObject.GetComponent<BoxCollider>().enabled = false;
                FullWallPrefab.SetActive(false);

                // Instantiate busted wall at the same position/rotation
                currentBustedWallInstance = Instantiate(
                    bustedWallPrefab,
                    bustedWallPrefab.transform.position,
                    bustedWallPrefab.transform.rotation,
                    transform.parent // optional: keep hierarchy
                );
                currentBustedWallInstance.SetActive(true);

                // Play sound effect
                if (wallAudioSource != null && breakClip != null)
                    wallAudioSource.PlayOneShot(breakClip);

                // Start reset coroutine
                StartCoroutine(ResetWallRoutine());
            }
        }
    }

    private IEnumerator ResetWallRoutine()
    {
        yield return new WaitForSeconds(30f);

        // Destroy the busted wall instance
        if (currentBustedWallInstance != null)
        {
            Destroy(currentBustedWallInstance);
            currentBustedWallInstance = null;
        }

        // Reactivate full wall and collider
        FullWallPrefab.SetActive(true);
        gameObject.GetComponent<BoxCollider>().enabled = true;
    }
}

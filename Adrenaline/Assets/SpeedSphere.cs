using UnityEngine;

public class SpeedSphere : MonoBehaviour
{
    [SerializeField] float speedGained;
    [SerializeField] float speedDuration;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            PlayerMovement playerMove = collision.gameObject.GetComponent<PlayerMovement>();

            if (playerMove != null)
            {
                StartCoroutine(ApplySpeedBoost(playerMove));
            }

            // Optional: disable pickup object after use
            gameObject.SetActive(false);
        }
    }
    private System.Collections.IEnumerator ApplySpeedBoost(PlayerMovement playerMove)
    {
        float originalRunSpeed = playerMove.runSpeed;
        float originalSprintSpeed = playerMove.sprintSpeed;
        playerMove.runSpeed += speedGained;
        playerMove.sprintSpeed += speedGained;

        yield return new WaitForSeconds(speedDuration);


        //wont work if you make other things that change speed
        playerMove.runSpeed -= speedGained;
        playerMove.sprintSpeed -= speedGained;

        // destroy or reactivate pickup if desired
        Destroy(gameObject);
    }
}

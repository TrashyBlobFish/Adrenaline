using UnityEngine;

public class Teleport : MonoBehaviour
{
    public GameObject EndPoint;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Teleport the player to a new position (for example, 10 units up)
            Vector3 newPosition = EndPoint.transform.position;
            collision.transform.position = newPosition;
        }
    }
}

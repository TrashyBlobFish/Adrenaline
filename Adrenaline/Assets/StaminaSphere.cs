using UnityEngine;

public class StaminaSphere : MonoBehaviour
{
    [SerializeField] float StaminaRegained;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<PlayerMovement>().currentStamina += StaminaRegained;
            Destroy(gameObject);
        }
    }
}

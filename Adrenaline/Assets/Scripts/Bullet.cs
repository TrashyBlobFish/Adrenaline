using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 50f;
    private Rigidbody rb;
    public GameObject Boss;
    public GameObject Boulder;

    void Start()
    {
        Boss = GameObject.Find("Boss");
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            //rb.linearVelocity = transform.forward * speed;
        }
        StartCoroutine(DestroyAfterTime(10f));
    }

    void OnCollisionEnter(Collision collision)
    {
        if (rb == null) return;

        if (collision.collider.CompareTag("Shield") && Boss != null)
        {
            Debug.Log("Bullet hit shield, redirecting toward Boss");

            // Calculate direction from bullet to boss
            Vector3 directionToBoss = (Boss.transform.position - transform.position).normalized;

            // Redirect bullet toward the boss
            rb.linearVelocity = directionToBoss * speed;

            // Rotate bullet to face the boss
            transform.forward = directionToBoss;
        }
        if (collision.gameObject == Boss)
        {
            Boulder = Boss.GetComponent<TutorialBoss>().Boulder;
            Boulder.GetComponent<ButtonActivator>().EnableObject();
            Boss.GetComponent<TutorialBoss>().SetPhase(2);
        }
        if (collision.collider.CompareTag("Player") && collision.gameObject != GameObject.Find("Shield"))
        {
            Boss.GetComponent<TutorialBoss>().SetPhase(1);
            GameObject.Find("Player").transform.position = GameObject.Find("Boss Respawn Point").transform.position;
            Destroy(gameObject);
        }
    }

    private System.Collections.IEnumerator DestroyAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        Destroy(gameObject);
    }
}
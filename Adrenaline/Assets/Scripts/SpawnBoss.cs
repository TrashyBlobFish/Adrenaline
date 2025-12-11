using UnityEngine;

public class SpawnBoss : MonoBehaviour
{
    public GameObject boss;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            boss.SetActive(true);
        }
    }
}

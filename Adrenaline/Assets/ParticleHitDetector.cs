using UnityEngine;

public class ParticleHitDetector : MonoBehaviour
{

    void OnParticleCollision(GameObject other)
    {
        Debug.Log("Particle hit something");
        // Check if we hit the player
        if (other == GameObject.Find("Player"))
        {
            Debug.Log("Particle hit the player!");
            gameObject.GetComponent<TutorialBoss>().SetPhase(1);
            other.transform.position = GameObject.Find("Boss Respawn Point").transform.position;

        }
    }
}

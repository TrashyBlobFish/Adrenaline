using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnPlayer : MonoBehaviour
{
    public GameObject playerPrefab;

    

    public void Spawn()
    {
        // Find all objects tagged as "SpawnPoint"
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");
        if (spawnPoints.Length == 0)
        {
            Debug.LogWarning("No SpawnPoints found in the scene!");
            return;
        }

        // Pick one at random
        GameObject chosenSpawn = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Instantiate the player at the chosen spawn point
        Instantiate(playerPrefab, chosenSpawn.transform.position, chosenSpawn.transform.rotation);
    }
}

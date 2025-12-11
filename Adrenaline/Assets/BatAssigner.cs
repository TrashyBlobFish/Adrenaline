using Unity.Netcode;
using UnityEngine;

public class BatAssigner : MonoBehaviour
{
    public GameObject baseballBatPrefab;
    private void OnCollisionEnter(Collision collision)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                AssignBatToRandomPlayer();
            }
            GameObject.Find("GameManager").GetComponent<GameManager>().StartMatch();
        }
        
    }

    void AssignBatToRandomPlayer()
    {
        var players = Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        if (players.Length == 0) return;

        int randomIndex = Random.Range(0, players.Length);
        var chosenPlayer = players[randomIndex];

        chosenPlayer.HasBaseballBat = true;

    }
}

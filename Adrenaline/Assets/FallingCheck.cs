using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class FallingCheck : MonoBehaviour
{
    [Header("If player falls below this Y value, teleport them here")]
    public float fallThreshold = -50f;

    private void Update()
    {
        // Find all players by tag (works with Netcode player prefabs)
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            NetworkObject netObj = player.GetComponent<NetworkObject>();
            if (netObj == null)
                continue;

            // Only teleport the LOCAL OWNED player
            if (!netObj.IsOwner)
                continue;

            // Check if they fell below the threshold
            if (player.transform.position.y < fallThreshold)
            {
                TeleportLocalPlayer(player);
            }
        }
    }

    private void TeleportLocalPlayer(GameObject player)
    {
        CharacterController cc = player.GetComponent<CharacterController>();

        if (cc != null)
            cc.enabled = false;

        player.transform.position = transform.position;
        player.transform.rotation = transform.rotation;

        if (cc != null)
            cc.enabled = true;
    }
}

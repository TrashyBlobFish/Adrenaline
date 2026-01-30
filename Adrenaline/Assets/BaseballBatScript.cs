using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class BaseballBatScript : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Bat triggered with: " + other.gameObject.name);

        // Check if the parent (player) is the owner
        var ownerNetObj = GetComponentInParent<NetworkObject>();
        if (ownerNetObj == null || !ownerNetObj.IsOwner) return;

        var player = other.gameObject.GetComponent<PlayerMovement>();
        if (player != null && !player.IsOwner && other.gameObject.CompareTag("Player"))
        {
            Debug.Log("Bat triggered a player: " + player.name);
            ulong targetId = player.NetworkObjectId;
            Vector3 launchVector = transform.forward * 40f + Vector3.up * 10f;

            var ownerPlayer = GetComponentInParent<PlayerMovement>();
            Debug.Log("Owner player: " + ownerPlayer);
            if (ownerPlayer != null)
            {
                ownerPlayer.RequestFlingPlayerServerRpc(targetId, launchVector);
            }
        }
    }


}
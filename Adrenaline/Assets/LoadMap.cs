using UnityEngine;
using Unity.Netcode;

public class LoadMap : NetworkBehaviour
{
    public string selectedMap;

    private void OnCollisionEnter(Collision collision)
    {
        // Only the server should react to collisions for scene loading
        if (!IsServer) return;
        
        if (collision.gameObject.CompareTag("Player"))
        {
            NetworkObject netObj = collision.gameObject.GetComponentInParent<NetworkObject>();
            if (netObj != null && netObj.IsOwner && netObj.IsOwnedByServer)
            {
                // Host player touched it -> load scene
                GetComponent<NetworkChangeScenes>().ChangeScene(selectedMap);
            }
        }
    }
}

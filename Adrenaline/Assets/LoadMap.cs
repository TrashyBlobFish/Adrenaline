using UnityEngine;
using Unity.Netcode;

public class LoadMap : NetworkBehaviour
{
    public string selectedMap;

    private void OnCollisionEnter(Collision collision)
    {
        
        if (!IsOwnedByServer) return;
        Debug.Log("Collision detected with: hopfully player");
        if (collision.gameObject.CompareTag("Player"))
        {
            NetworkObject netObj = collision.gameObject.GetComponentInParent<NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                // Player (any owner) touched it -> load scene
                gameObject.GetComponent<NetworkChangeScenes>().ChangeScene(selectedMap);
            }
        }
    }
}

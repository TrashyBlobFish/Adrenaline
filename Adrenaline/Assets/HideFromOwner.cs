using Unity.Netcode;
using UnityEngine;

public class HideFromOwner : NetworkBehaviour
{
    private void Start()
    {
        if (IsOwner)
        {
            gameObject.SetActive(false);
        }
    }
}

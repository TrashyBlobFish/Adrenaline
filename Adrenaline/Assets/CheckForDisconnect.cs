using UnityEngine;

public class CheckForDisconnect : MonoBehaviour
{
    public GameObject DisconnectUI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (DisconnectUI.activeSelf)
            gameObject.SetActive(false);
    }
}

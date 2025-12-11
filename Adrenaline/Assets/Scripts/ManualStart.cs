using UnityEngine;

public class ManualStart : MonoBehaviour
{
    public TestRelay relay;
    public Camera startcam;
    public void ManualyStartGame()
    {
        relay.CreateRelay();
        startcam.gameObject.SetActive(false);
    }
}

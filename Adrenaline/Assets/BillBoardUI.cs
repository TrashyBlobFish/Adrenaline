using UnityEngine;

public class BillBoardUI : MonoBehaviour
{
    private Camera playerCamera;

    void Start()
    {
        playerCamera = Camera.main;
    }

    void Update()
    {
        FacePlayerHorizontally();
    }

    private void FacePlayerHorizontally()
    {
        if (playerCamera == null)
            return;

        Vector3 directionToPlayer = playerCamera.transform.position - transform.position;
        directionToPlayer.y = 0; // Keep only horizontal direction

        if (directionToPlayer.magnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(directionToPlayer);
        }
    }
}

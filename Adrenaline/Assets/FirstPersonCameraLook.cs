using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonCameraLook : MonoBehaviour
{
    public Transform playerBody; // Reference to the player's body (used for horizontal rotation)
    public CameraModeSwitcher playercameraswitcher;
    public float lookSpeedX = 2f; // Mouse sensitivity for horizontal rotation
    public float lookSpeedY = 2f; // Mouse sensitivity for vertical rotation
    public float upperLimit = -80f; // Max vertical angle (looking up)
    public float lowerLimit = 80f; // Max vertical angle (looking down)

    private float currentRotationX = 0f; // Current vertical rotation (used for limiting)
    private float currentRotationY = 0f; // Current horizontal rotation (used for controlling direction)

    void FixedUpdate()
    {
        if (playercameraswitcher.isThirdPerson == false)
        {
            Vector3 currentRotation = playerBody.transform.eulerAngles;
            float targetY = playercameraswitcher.firstPersonVCam.transform.eulerAngles.y;

            playerBody.transform.rotation = Quaternion.Euler(currentRotation.x, targetY, currentRotation.z);
        }
            
        
        
    }
}

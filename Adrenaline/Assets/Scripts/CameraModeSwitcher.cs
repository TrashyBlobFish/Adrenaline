using UnityEngine;
using Unity.Cinemachine;

public class CameraModeSwitcher : MonoBehaviour
{
    private UserProfileData userProfile;
    public CinemachineCamera thirdPersonVCam;
    public CinemachineCamera firstPersonVCam;
    
    // Add reference to your Pause menu (you'll need to assign this in the inspector or find it)
    public PauseMenuUI pauseMenu;

    // Component references
    private CinemachineInputAxisController thirdPersonInput;
    private CinemachineInputAxisController firstPersonInput;

    public bool isThirdPerson = true;

    void Start()
    {
        userProfile = Object.FindFirstObjectByType<UserProfileData>();
        
        // If PauseMenuUI isn't assigned, try to find it
        if (pauseMenu == null)
            pauseMenu = Object.FindFirstObjectByType<PauseMenuUI>();

        // Cache the input axis controllers to toggle them later
        if (thirdPersonVCam != null)
            thirdPersonInput = thirdPersonVCam.GetComponent<CinemachineInputAxisController>();
            
        if (firstPersonVCam != null)
            firstPersonInput = firstPersonVCam.GetComponent<CinemachineInputAxisController>();

        SetCameraMode(isThirdPerson);
    }

    void Update()
    {
        // Toggle input reading based on the pause menu state
        bool isPaused = pauseMenu != null && pauseMenu.isMenuOpen;

        if (thirdPersonInput != null)
            thirdPersonInput.enabled = !isPaused;
            
        if (firstPersonInput != null)
            firstPersonInput.enabled = !isPaused;

        // Skip other updates while paused
        if (isPaused) return;

        if (Input.GetKeyDown(KeyCode.V))
        {
            isThirdPerson = !isThirdPerson;
            SetCameraMode(isThirdPerson);
        }
        
        // When in third person
        if (userProfile != null && isThirdPerson)
            userProfile.Timespent3rdPerson += Time.deltaTime;

        // When in first person
        if (userProfile != null && !isThirdPerson)
            userProfile.Timespent1stPerson += Time.deltaTime;
    }

    void SetCameraMode(bool thirdPerson)
    {
        if (thirdPerson)
        {
            thirdPersonVCam.Priority = 30;
            firstPersonVCam.Priority = 0;
        }
        else
        {
            thirdPersonVCam.Priority = 0;
            firstPersonVCam.Priority = 30;
        }
    }
}
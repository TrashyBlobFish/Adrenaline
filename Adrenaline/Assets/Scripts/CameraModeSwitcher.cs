using UnityEngine;
using Unity.Cinemachine;

public class CameraModeSwitcher : MonoBehaviour
{
    private UserProfileData userProfile;
    public CinemachineCamera thirdPersonVCam;
    public CinemachineCamera firstPersonVCam;

    public bool isThirdPerson = true;

    void Start()
    {
        userProfile = Object.FindFirstObjectByType<UserProfileData>();
        SetCameraMode(isThirdPerson);
    }

    void Update()
    {
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
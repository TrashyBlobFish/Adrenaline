using UnityEngine;
using Unity.Cinemachine;

public class CameraModeSwitcher : MonoBehaviour
{
    public CinemachineCamera thirdPersonVCam;
    public CinemachineCamera firstPersonVCam;

    public bool isThirdPerson = true;

    void Start()
    {
        SetCameraMode(isThirdPerson);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            isThirdPerson = !isThirdPerson;
            SetCameraMode(isThirdPerson);
        }
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
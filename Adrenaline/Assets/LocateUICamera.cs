using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LocateUICamera : MonoBehaviour
{
    private void Awake()
    {
        Camera uiCamera = GameObject.Find("CameraUI").GetComponent<Camera>();
        var baseCamData = gameObject.GetComponent<Camera>().GetUniversalAdditionalCameraData();
        var overlayCamData = uiCamera.GetUniversalAdditionalCameraData();

        if (uiCamera != null)
        {
            if (!baseCamData.cameraStack.Contains(uiCamera))
            {
                baseCamData.cameraStack.Add(uiCamera);
            }
        }
        else
        {
            Debug.LogError("No Camera component found on this GameObject.");
        }
    }
}

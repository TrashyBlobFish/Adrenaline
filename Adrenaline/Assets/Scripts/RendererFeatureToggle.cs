using UnityEngine;
using UnityEngine.Rendering.Universal;

public class RendererFeatureToggle : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private int xrayRendererIndex = 0;
    [SerializeField] private int normalRendererIndex = 1;
    
    private UniversalAdditionalCameraData cameraData;

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        cameraData = targetCamera.GetComponent<UniversalAdditionalCameraData>();
        if (cameraData == null)
        {
            Debug.LogError("[RendererFeatureToggle] UniversalAdditionalCameraData not found on camera!");
            return;
        }

        // Set default renderer (normal without x-ray)
        cameraData.SetRenderer(normalRendererIndex);
        Debug.Log("[RendererFeatureToggle] Initialized with normal renderer (index " + normalRendererIndex + ").");
    }

    public void SetFeatureEnabled(bool enabled)
    {
        if (cameraData == null)
        {
            Debug.LogError("[RendererFeatureToggle] Camera data not initialized!");
            return;
        }
        int targetRendererIndex = enabled ? xrayRendererIndex : normalRendererIndex;
        cameraData.SetRenderer(targetRendererIndex);
        Debug.Log($"[RendererFeatureToggle] Switched to {(enabled ? "X-Ray" : "Normal")} renderer (index {targetRendererIndex}).");
    }
}
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Splines;

public sealed class MenuCameraSwitcher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineSplineDolly dolly;
    [SerializeField] private SplineContainer splineContainer;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;

    [Header("Startup")]
    [SerializeField] private int startPointIndex;
    [SerializeField] private bool snapToStartPointOnAwake = true;

    public int CurrentPointIndex { get; private set; } = -1;

    private int targetPointIndex = -1;
    private float targetCameraPosition;
    private bool isMovingToPoint;

    private void Reset()
    {
        dolly = GetComponent<CinemachineSplineDolly>();
        splineContainer = GetComponent<SplineContainer>();
    }

    private void Awake()
    {
        if (dolly == null)
        {
            dolly = GetComponent<CinemachineSplineDolly>();
        }
    }

    private void Start()
    {
        if (!TryGetSpline(out Spline spline))
        {
            return;
        }

        int knotCount = spline.Count;
        if (knotCount == 0)
        {
            return;
        }

        startPointIndex = Mathf.Clamp(startPointIndex, 0, knotCount - 1);

        if (snapToStartPointOnAwake)
        {
            SnapToPoint(startPointIndex);
        }
        else
        {
            SwitchToPoint(startPointIndex);
        }
    }

    private void LateUpdate()
    {
        if (!isMovingToPoint || dolly == null)
        {
            return;
        }

        dolly.CameraPosition = Mathf.MoveTowards(
            dolly.CameraPosition,
            targetCameraPosition,
            moveSpeed * Time.deltaTime
        );

        if (Mathf.Approximately(dolly.CameraPosition, targetCameraPosition))
        {
            dolly.CameraPosition = targetCameraPosition;
            CurrentPointIndex = targetPointIndex;
            targetPointIndex = -1;
            isMovingToPoint = false;
        }
    }

    public void SwitchToPoint(int pointIndex)
    {
        if (!TryGetPointPosition(pointIndex, out float cameraPosition))
        {
            return;
        }

        targetPointIndex = pointIndex;
        targetCameraPosition = cameraPosition;
        isMovingToPoint = true;
    }

    public void SnapToPoint(int pointIndex)
    {
        if (!TryGetPointPosition(pointIndex, out float cameraPosition))
        {
            return;
        }

        if (dolly == null)
        {
            return;
        }

        dolly.CameraPosition = cameraPosition;
        targetCameraPosition = cameraPosition;
        CurrentPointIndex = pointIndex;
        targetPointIndex = -1;
        isMovingToPoint = false;
    }

    public void NextPoint()
    {
        if (!TryGetSpline(out Spline spline))
        {
            return;
        }

        int knotCount = spline.Count;
        if (knotCount == 0)
        {
            return;
        }

        int nextIndex = CurrentPointIndex + 1;
        if (nextIndex >= knotCount)
        {
            nextIndex = 0;
        }

        SwitchToPoint(nextIndex);
    }

    public void PreviousPoint()
    {
        if (!TryGetSpline(out Spline spline))
        {
            return;
        }

        int knotCount = spline.Count;
        if (knotCount == 0)
        {
            return;
        }

        int previousIndex = CurrentPointIndex - 1;
        if (previousIndex < 0)
        {
            previousIndex = knotCount - 1;
        }

        SwitchToPoint(previousIndex);
    }

    private bool TryGetSpline(out Spline spline)
    {
        spline = null;

        if (splineContainer == null || splineContainer.Splines.Count == 0)
        {
            return false;
        }

        spline = splineContainer.Splines[0];
        return spline != null;
    }

    private bool TryGetPointPosition(int pointIndex, out float cameraPosition)
    {
        cameraPosition = 0f;

        if (!TryGetSpline(out Spline spline))
        {
            return false;
        }

        int knotCount = spline.Count;
        if (knotCount == 0 || pointIndex < 0 || pointIndex >= knotCount)
        {
            return false;
        }

        cameraPosition = knotCount > 1
            ? pointIndex / (float)(knotCount - 1)
            : 0f;

        return true;
    }
}
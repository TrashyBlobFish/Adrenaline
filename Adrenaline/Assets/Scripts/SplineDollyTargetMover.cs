using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Splines;

public sealed class SplineDollyTargetMover : MonoBehaviour
{
    [SerializeField] private CinemachineSplineDolly dolly;
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private float moveSpeed = 2f;

    private float targetCameraPosition;
    private bool isMovingToKnot;

    private void Reset()
    {
        dolly = GetComponent<CinemachineSplineDolly>();
    }

    private void Update()
    {
        if (!isMovingToKnot || dolly == null)
        {
            return;
        }

        dolly.CameraPosition = Mathf.MoveTowards(dolly.CameraPosition, targetCameraPosition, moveSpeed * Time.deltaTime);

        if (Mathf.Approximately(dolly.CameraPosition, targetCameraPosition))
        {
            isMovingToKnot = false;
        }
    }

    /// <summary>
    /// Move the dolly smoothly to a specific knot on the spline.
    /// </summary>
    /// <param name="knotIndex">The index of the knot (0 to number of knots - 1)</param>
    public void MoveToKnot(int knotIndex)
    {
        if (dolly == null || splineContainer == null)
        {
            return;
        }

        if (splineContainer.Splines.Count == 0)
        {
            Debug.LogWarning("No splines in spline container.");
            return;
        }

        var spline = splineContainer.Splines[0];
        int knotCount = spline.Count;

        if (knotCount == 0 || knotIndex < 0 || knotIndex >= knotCount)
        {
            Debug.LogWarning($"Invalid knot index: {knotIndex}. Spline has {knotCount} knots.");
            return;
        }

        targetCameraPosition = knotCount > 1 ? knotIndex / (float)(knotCount - 1) : 0f;
        isMovingToKnot = true;
    }
}
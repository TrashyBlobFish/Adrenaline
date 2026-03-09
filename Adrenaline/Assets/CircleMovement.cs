using UnityEngine;

public class CircleMovement : MonoBehaviour
{
    public float radius = 5f;       // Circle size
    public float speed = 1f;        // Rotation speed

    private float angle;
    private Vector3 centerPoint;
    private Vector3 previousPosition;

    void Start()
    {
        // Store the starting position as the center of the circle
        centerPoint = transform.position;
        previousPosition = transform.position;
    }

    void Update()
    {
        angle += speed * Time.deltaTime;

        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;

        transform.position = centerPoint + new Vector3(x, 0, z);

        // Calculate movement direction and rotate to face it
        Vector3 direction = transform.position - previousPosition;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        previousPosition = transform.position;
    }
}
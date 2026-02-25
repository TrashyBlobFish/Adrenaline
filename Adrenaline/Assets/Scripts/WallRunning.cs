using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallRunning : MonoBehaviour
{
    [Header("Wallrunning")]
    public LayerMask whatIsWall;
    public LayerMask whatIsGround;
    public float wallRunForce;
    public float maxWallRunSpeed;
    public float wallClimbSpeed;
    public float maxWallRunTime;
    private float wallRunTimer;

    [Header("Input")]
    public KeyCode upwardsRunKey = KeyCode.LeftShift;
    public KeyCode downwardsRunKey = KeyCode.LeftControl;
    private bool upwardsRunning;
    private bool downwardsRunning;
    private float horizontalInput;
    private float verticalInput;

    [Header("Detection")]
    public float wallCheckDistance;
    public float minJumpHeight;
    private RaycastHit leftWallhit;
    private RaycastHit rightWallhit;
    private bool wallLeft;
    private bool wallRight;

    [Header("Attachment Settings")]
    public float minAttachSpeed = 5f; // Minimum speed to start wall-running
    public float exitForce = 5f; // Force applied when exiting wall-run
    public bool requireInputTowardsWall = true; // Must press towards wall to attach
    
    [Header("References")]
    public Transform orientation;
    private PlayerMovement pm;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        CheckForWall();

        // Update input values so IsWallRunningPossible() works
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        upwardsRunning = Input.GetKey(upwardsRunKey);
        downwardsRunning = Input.GetKey(downwardsRunKey);
    }

    private void FixedUpdate()
    {
        if (pm.wallrunning)
            WallRunningMovement();
    }

    private void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallhit, wallCheckDistance, whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallhit, wallCheckDistance, whatIsWall);
    }

    private bool AboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, whatIsGround);
    }

    private void StartWallRun()
    {
        pm.wallrunning = true;
    }

    private void WallRunningMovement()
    {
        rb.useGravity = false;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        Vector3 wallNormal = wallRight ? rightWallhit.normal : leftWallhit.normal;

        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        if ((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
            wallForward = -wallForward;

        // forward force
        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);

        // upwards/downwards force
        if (upwardsRunning)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, wallClimbSpeed, rb.linearVelocity.z);
        if (downwardsRunning)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, -wallClimbSpeed, rb.linearVelocity.z);

        // push to wall force
        if (!(wallLeft && horizontalInput > 0) && !(wallRight && horizontalInput < 0))
            rb.AddForce(-wallNormal * 100, ForceMode.Force);
        
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (horizontalVelocity.magnitude > maxWallRunSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * maxWallRunSpeed;
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
        }
    }

    public void StopWallRun()
    {
        pm.wallrunning = false;
        rb.useGravity = true;
        if (wallRight)
            rb.AddForce(rightWallhit.normal * exitForce, ForceMode.VelocityChange);
        else if (wallLeft)
            rb.AddForce(leftWallhit.normal * exitForce, ForceMode.VelocityChange);
    }

    public bool IsWallRunningPossible()
    {
        // Must be next to a wall and above ground
        if (!(wallLeft || wallRight) || !AboveGround())
            return false;

        // Check if player has minimum speed to attach
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (horizontalVelocity.magnitude < minAttachSpeed)
            return false;

        // Require intentional input towards the wall
        if (requireInputTowardsWall)
        {
            // Player must be pressing towards the wall (not away from it)
            if (wallRight && horizontalInput < 0) // Wall on right, pressing left (away)
                return false;
            if (wallLeft && horizontalInput > 0) // Wall on left, pressing right (away)
                return false;

            // Optional: Also require forward input for more intentional feel
            if (verticalInput <= 0) // Not pressing forward
                return false;
        }

        return true;
    }

    public bool ShouldExitWallRun()
    {
        // Exit if no longer next to wall
        if (!wallLeft && !wallRight)
            return true;

        // Exit if player is grounded
        if (!AboveGround())
            return true;

        // Exit if player actively presses away from wall
        if (wallRight && horizontalInput < -0.5f) // Pressing strongly left
            return true;
        if (wallLeft && horizontalInput > 0.5f) // Pressing strongly right
            return true;

        // Exit if player stops pressing forward
        if (verticalInput < -0.5f) // Pressing backward
            return true;

        return false;
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.IO.LowLevel.Unsafe;

public class PlayerStateMachine : NetworkBehaviour
{
    public PlayerMovement movement;
    public WallRunning wallRun;
    [HideInInspector] public PlayerBaseState currentState;

    public IdleState idleState = new IdleState();
    public RunningState runningState = new RunningState();
    public SprintingState sprintingState = new SprintingState();
    public JumpingState jumpingState = new JumpingState();
    public WallRunningState wallRunningState = new WallRunningState();
    private NetworkObject rootNetworkObject;

    private void Awake()
    {
        wallRun = movement.GetComponent<WallRunning>();
        movement = GetComponent<PlayerMovement>();
    }

    private void Start()
    {
        
        rootNetworkObject = GetComponentInParent<NetworkObject>();
        currentState = idleState;
        currentState.EnterState(this);
    }

    private void Update()
    {
        if (!rootNetworkObject.IsOwner)
        {
            movement.enabled = false;
            return;
        }

        currentState.UpdateState(this);

        Vector2 moveInput = movement.playerInput.actions["Move"].ReadValue<Vector2>();
        bool sprintInput = movement.playerInput.actions["Sprint"].IsPressed();
        bool jumpInput = movement.playerInput.actions["Jump"].WasPressedThisFrame();


        WallRunning wallRun = movement.GetComponent<WallRunning>();
        if (wallRun != null)
        {
            // Check if we should exit wall-running
            if (currentState == wallRunningState && wallRun.ShouldExitWallRun())
            {
                // Exit wall-running, let normal state logic take over
                wallRun.StopWallRun();
                // Don't return, let the state machine transition normally
            }
            // Check if we should enter wall-running
            else if (wallRun.IsWallRunningPossible() && currentState != wallRunningState)
            {
                SwitchState(wallRunningState);
                return;
            }
            // Stay in wall-running if already there and conditions still met
            else if (currentState == wallRunningState)
            {
                return;
            }
        }

        // Jump check comes first
        if (jumpInput && movement.isGrounded)
        {
            SwitchState(jumpingState);
        }
        else if (moveInput.magnitude < 0.1f)
        {
            SwitchState(idleState);
        }
        else if (sprintInput)
        {
            SwitchState(sprintingState);
        }
        else
        {
            SwitchState(runningState);
        }
    }

    private void FixedUpdate()
    {
        if (!rootNetworkObject.IsOwner)
            return;

        currentState.FixedUpdateState(this);
    }

    public void SwitchState(PlayerBaseState newState)
    {
        if (currentState == newState) return;
        currentState.ExitState(this);
        currentState = newState;
        currentState.EnterState(this);

        //used for debugging state changes
        Debug.Log($"Current State: {currentState.GetType().Name}");
    }
}

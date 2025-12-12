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
        if (wallRun != null && wallRun.IsWallRunningPossible() && movement.hasStaminaToActivate)
        {
            if (currentState != wallRunningState)
            {
                SwitchState(wallRunningState);
                return;
            }
        }

        // Jump check comes first
        if (jumpInput && movement.isGrounded)
        {
            SwitchState(jumpingState);
        }else if (moveInput.magnitude < 0.1f)
        {
            SwitchState(idleState);
        }
        else if (sprintInput && movement.hasStaminaToActivate)
        {
            SwitchState(sprintingState);
        }
        else
        {
            SwitchState(runningState);
        }
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

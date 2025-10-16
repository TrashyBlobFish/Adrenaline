using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.IO.LowLevel.Unsafe;

public class PlayerStateMachine : NetworkBehaviour
{
    [HideInInspector] public PlayerMovement movement;
    [HideInInspector] public PlayerBaseState currentState;

    public IdleState idleState = new IdleState();
    public RunningState runningState = new RunningState();
    public SprintingState sprintingState = new SprintingState();
    public JumpingState jumpingState = new JumpingState(); // 👈 NEW

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
    }

    private void Start()
    {
        currentState = idleState;
        currentState.EnterState(this);
    }

    private void Update()
    {
        if (!IsOwner) return;

        currentState.UpdateState(this);

        Vector2 moveInput = movement.playerInput.actions["Move"].ReadValue<Vector2>();
        bool sprintInput = movement.playerInput.actions["Sprint"].IsPressed();
        bool jumpInput = movement.playerInput.actions["Jump"].WasPressedThisFrame();

        // Jump check comes first
        if (jumpInput && movement.isGrounded)
        {
            SwitchState(jumpingState);
        }
        else if (moveInput.magnitude < 0.1f)
        {
            SwitchState(idleState);
        }
        else if (sprintInput && movement.hasStam)
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

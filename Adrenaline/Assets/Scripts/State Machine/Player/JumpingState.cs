using UnityEngine;

public class JumpingState : PlayerBaseState
{
    private bool hasJumped = false;
    private float jumpTimer = 0.2f; // short timer before returning control

    public override void EnterState(PlayerStateMachine player)
    {
        hasJumped = false;

        // Only allow jump if grounded
        if (player.movement.isGrounded)
        {
            Rigidbody rb = player.movement.GetComponent<Rigidbody>();
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * player.movement.jumpHeight, ForceMode.Impulse);
            hasJumped = true;
        }
    }

    public override void UpdateState(PlayerStateMachine player)
    {
        jumpTimer -= Time.deltaTime;

        // While mid-air, keep some horizontal control
        player.movement.HandleMovement();

        // Transition logic
        if (player.movement.isGrounded && jumpTimer <= 0f)
        {
            Vector2 moveInput = player.movement.playerInput.actions["Move"].ReadValue<Vector2>();
            bool sprintInput = player.movement.playerInput.actions["Sprint"].IsPressed();

            if (moveInput.magnitude < 0.1f)
                player.SwitchState(player.idleState);
            else if (sprintInput && player.movement.hasStam)
                player.SwitchState(player.sprintingState);
            else
                player.SwitchState(player.runningState);
        }
    }

    public override void ExitState(PlayerStateMachine player)
    {
        hasJumped = false;
    }
}

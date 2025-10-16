using UnityEngine;

public class IdleState : PlayerBaseState
{
    public override void EnterState(PlayerStateMachine player)
    {
        // Stop movement when entering idle
        Rigidbody rb = player.movement.GetComponent<Rigidbody>();
        Vector3 stopVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        rb.linearVelocity = stopVelocity;
    }

    public override void UpdateState(PlayerStateMachine player)
    {
        // Nothing much happens in idle, handled by state machine transitions
    }

    public override void ExitState(PlayerStateMachine player)
    {
        // Nothing needed yet
    }
}

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
        // Regenerate stamina when not sprinting
        player.movement.currentStamina += player.movement.staminaRegenRate * Time.deltaTime;
        player.movement.currentStamina = Mathf.Min(player.movement.currentStamina, player.movement.maxStamina);
    }

    public override void ExitState(PlayerStateMachine player)
    {
        // Nothing needed yet
    }
}

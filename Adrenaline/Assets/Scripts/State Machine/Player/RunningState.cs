using UnityEngine;

public class RunningState : PlayerBaseState
{
    public override void EnterState(PlayerStateMachine player)
    {
        player.movement.movementspeed = player.movement.runSpeed;
    }

    public override void UpdateState(PlayerStateMachine player)
    {
        
        player.movement.HandleMovement();

        // Regenerate stamina when not sprinting
        player.movement.currentStamina += player.movement.staminaRegenRate * Time.deltaTime;
        player.movement.currentStamina = Mathf.Min(player.movement.currentStamina, player.movement.maxStamina);
    }

    public override void ExitState(PlayerStateMachine player) { }
}

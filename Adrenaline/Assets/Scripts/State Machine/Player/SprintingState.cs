using UnityEngine;

public class SprintingState : PlayerBaseState
{
    public override void EnterState(PlayerStateMachine player)
    {
        player.movement.movementspeed = player.movement.sprintSpeed;
    }

    public override void UpdateState(PlayerStateMachine player)
    {
        player.movement.HandleMovement();

        player.movement.currentStamina -= player.movement.staminaDrainRate * Time.deltaTime;
        player.movement.currentStamina = Mathf.Max(0, player.movement.currentStamina);
    }

    public override void ExitState(PlayerStateMachine player) { }
}

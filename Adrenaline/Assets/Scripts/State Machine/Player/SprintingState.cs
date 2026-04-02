using UnityEngine;

public class SprintingState : PlayerBaseState
{
    public override void EnterState(PlayerStateMachine player)
    {
        if (!player.movement.hasStaminaToActivate)
        {
            player.SwitchState(player.runningState);
            return;
        }
        player.movement.movementspeed = player.movement.sprintSpeed;
    }

    public override void UpdateState(PlayerStateMachine player)
    {
        player.movement.currentStamina -= player.movement.staminaDrainRate * Time.deltaTime;
        player.movement.currentStamina = Mathf.Max(0, player.movement.currentStamina);
        if (!player.movement.hasStaminaToUse)
        {
            player.SwitchState(player.runningState);
        }
    }

    public override void FixedUpdateState(PlayerStateMachine player)
    {
        player.movement.HandleMovement();
    }

    public override void ExitState(PlayerStateMachine player) { }
}

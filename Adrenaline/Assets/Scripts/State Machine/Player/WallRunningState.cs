using UnityEngine;

public class WallRunningState : PlayerBaseState
{
    private float maxWallRunSpeed = 10f; // Adjust as needed

    public override void EnterState(PlayerStateMachine player)
    {
        // Only require minStaminaToActivate to start wallrunning
        if (!player.movement.hasStaminaToActivate)
        {
            player.SwitchState(player.idleState);
            return;
        }
        player.movement.wallrunning = true;
    }

    public override void UpdateState(PlayerStateMachine player)
    {
        // Drain stamina
        player.movement.currentStamina -= player.movement.staminaDrainRate * Time.deltaTime;
        player.movement.currentStamina = Mathf.Max(0f, player.movement.currentStamina);

        // Only require minStaminaToUse to continue wallrunning
        if (!player.movement.hasStaminaToUse)
        {
            player.SwitchState(player.idleState);
            return;
        }

        // Exit if wall running is no longer possible
        WallRunning wallRun = player.movement.GetComponent<WallRunning>();
        if (wallRun == null || !wallRun.IsWallRunningPossible())
        {
            player.SwitchState(player.idleState);
        }
    }

    public override void ExitState(PlayerStateMachine player)
    {
        WallRunning wallRun = player.movement.GetComponent<WallRunning>();
        if (wallRun != null)
            wallRun.StopWallRun();
    }
}
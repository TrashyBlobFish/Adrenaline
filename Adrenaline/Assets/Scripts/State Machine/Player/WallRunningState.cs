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
            player.SwitchState(player.runningState);
            return;
        }

        // Exit if wall running is no longer possible
        WallRunning wallRun = player.movement.GetComponent<WallRunning>();
        if (wallRun == null || !wallRun.IsWallRunningPossible())
        {
            player.SwitchState(player.runningState);
        }
    }

    public override void ExitState(PlayerStateMachine player)
    {
        WallRunning wallRun = player.movement.GetComponent<WallRunning>();
        if (wallRun != null)
            wallRun.StopWallRun();

        // Preserve momentum: project current velocity onto the ground plane
        var rb = player.movement.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 horizontalVelocity = rb.linearVelocity;
            horizontalVelocity.y = 0f;
            if (horizontalVelocity.magnitude > 0.1f)
            {
                // Optionally, keep the horizontal velocity and let gravity handle the rest
                rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
            }
            // Optionally, add a small forward boost or jump-off
            // rb.AddForce(player.transform.forward * 5f + Vector3.up * 2f, ForceMode.VelocityChange);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SprintParticleController : MonoBehaviour
{
    public ParticleSystem sprintParticles;
    private ParticleSystem.EmissionModule emissionModule;

    // Store the original emission settings for re-enabling
    private float originalRateOverTime;
    private float originalRateOverDistance;

    private void Start()
    {
        // Get the EmissionModule from the particle system
        emissionModule = sprintParticles.emission;

        // Store the original emission settings
        originalRateOverTime = emissionModule.rateOverTime.constant;
        originalRateOverDistance = emissionModule.rateOverDistance.constant;

        
    }

    private void Update()
    {
        // Check if the left shift key is pressed
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (!sprintParticles.isPlaying)
            {
                sprintParticles.Play(); // Start emitting particles
            }

            // Re-enable emissions by restoring the original emission rates
            emissionModule.rateOverTime = originalRateOverTime;
            emissionModule.rateOverDistance = originalRateOverDistance;
        }
        else
        {
            // Immediately stop new emissions
            emissionModule.rateOverTime = 0f; // Stop emitting new particles over time
            emissionModule.rateOverDistance = 0f; // Stop emitting new particles over distance

            // If the particle system is still playing but no particles are left, stop it
            if (sprintParticles.isPlaying && sprintParticles.particleCount == 0)
            {
                sprintParticles.Clear(); // Clear remaining particles
                sprintParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // Stop emission and clear
            }
        }
    }
}

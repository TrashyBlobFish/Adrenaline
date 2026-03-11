using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SprintParticleController : NetworkBehaviour
{
    private GameObject playerUICamera;
    public ParticleSystem SpeedLines;
    public ParticleSystem sprintParticles;
    public PlayerMovement playerMovement;
    private Rigidbody rb;

    [Header("Speed Settings")]
    public float minSpeedForParticles = 1f; // Minimum speed to emit any particles
    public float maxSpeedForMaxBurst = 20f; // Speed at which burst frequency is at maximum
    
    [Header("Burst Settings")]
    public float minBurstCount = 1f; // Minimum particles per burst
    public float maxBurstCount = 10f; // Maximum particles per burst
    public float minBurstInterval = 0.5f; // Time between bursts at low speed
    public float maxBurstInterval = 0.05f; // Time between bursts at high speed

    [Header("Speed Lines Settings")]
    public float speedLinesActivationSpeed = 17f; // Speed to activate UI speed lines

    [Header("Network Sync")]
    public float syncInterval = 0.1f; // How often to sync particle state

    private float timeSinceLastBurst = 0f;
    private float timeSinceLastSync = 0f;
    private float currentBurstInterval;
    private bool wasEmitting = false;
    private bool isCurrentlyEmitting = false;
    
    // Remote client tracking
    private float remoteCurrentSpeed = 0f;
    private bool remoteIsEmitting = false;

    private void Start()
    {
        rb = GetComponentInParent<Rigidbody>();

        // Disable emission over time/distance, we'll use manual emission only
        var emissionModule = sprintParticles.emission;
        emissionModule.rateOverTime = 0f;
        emissionModule.rateOverDistance = 0f;
        emissionModule.enabled = false; // Completely disable auto emission
        
        // Only find UI camera for the local player (speed lines are UI only)
        if (IsOwner)
        {
            if (playerUICamera == null)
            {
                playerUICamera = GameObject.Find("CameraUI");
                if (playerUICamera != null)
                {
                    SpeedLines = playerUICamera.GetComponentInChildren<ParticleSystem>();
                }
            }
        }
        else
        {
            // For remote players, disable speed lines (UI effect only)
            SpeedLines = null;
        }

        // Ensure particle system starts stopped
        if (sprintParticles.isPlaying)
        {
            sprintParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            HandleOwnerUpdate();
        }
        else
        {
            HandleRemoteClientUpdate();
        }
    }

    private void HandleOwnerUpdate()
    {
        // Get the player's current horizontal speed
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        // Calculate if we should be emitting
        isCurrentlyEmitting = currentSpeed >= minSpeedForParticles;

        // Sync state change to all clients
        timeSinceLastSync += Time.deltaTime;
        if (timeSinceLastSync >= syncInterval)
        {
            if (isCurrentlyEmitting != wasEmitting)
            {
                if (isCurrentlyEmitting)
                {
                    StartParticlesServerRpc(currentSpeed);
                }
                else
                {
                    StopParticlesServerRpc();
                }
                wasEmitting = isCurrentlyEmitting;
            }
            else if (isCurrentlyEmitting)
            {
                // Update speed for already emitting particles
                UpdateParticleSpeedServerRpc(currentSpeed);
            }
            timeSinceLastSync = 0f;
        }

        // Handle local particle emission
        HandleParticleEmission(currentSpeed);

        // Handle speed lines (UI effect) - only for local player
        bool isHighSpeed = currentSpeed >= speedLinesActivationSpeed;
        if (isHighSpeed)
        {
            if (SpeedLines != null && !SpeedLines.isPlaying)
            {
                SpeedLines.Play();
            }
        }
        else
        {
            if (SpeedLines != null && SpeedLines.isPlaying)
            {
                SpeedLines.Stop();
            }
        }
    }

    private void HandleRemoteClientUpdate()
    {
        // Remote clients continuously emit based on synced state
        if (remoteIsEmitting)
        {
            HandleParticleEmission(remoteCurrentSpeed);
        }
    }

    private void HandleParticleEmission(float currentSpeed)
    {
        if (currentSpeed >= minSpeedForParticles)
        {
            // Calculate normalized speed (0 to 1)
            float speedNormalized = Mathf.Clamp01(currentSpeed / maxSpeedForMaxBurst);

            // Calculate burst interval (faster speed = more frequent bursts)
            currentBurstInterval = Mathf.Lerp(minBurstInterval, maxBurstInterval, speedNormalized);

            // Calculate burst count (faster speed = more particles per burst)
            float burstCount = Mathf.Lerp(minBurstCount, maxBurstCount, speedNormalized);

            timeSinceLastBurst += Time.deltaTime;

            // Emit burst based on interval
            if (timeSinceLastBurst >= currentBurstInterval)
            {
                sprintParticles.Emit(Mathf.RoundToInt(burstCount));
                timeSinceLastBurst = 0f;
            }

            // Start the particle system if not playing
            if (!sprintParticles.isPlaying)
            {
                sprintParticles.Play();
            }
        }
        else
        {
            // Reset burst timer when not moving
            timeSinceLastBurst = 0f;

            // Stop emitting
            if (sprintParticles.isPlaying)
            {
                sprintParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            // Clear particles completely when fully stopped and no particles remain
            if (sprintParticles.isPlaying && sprintParticles.particleCount == 0)
            {
                sprintParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    [ServerRpc]
    private void StartParticlesServerRpc(float speed)
    {
        StartParticlesClientRpc(speed);
    }

    [ClientRpc]
    private void StartParticlesClientRpc(float speed)
    {
        if (!IsOwner) // Only affect remote clients
        {
            remoteIsEmitting = true;
            remoteCurrentSpeed = speed;
        }
    }

    [ServerRpc]
    private void UpdateParticleSpeedServerRpc(float speed)
    {
        UpdateParticleSpeedClientRpc(speed);
    }

    [ClientRpc]
    private void UpdateParticleSpeedClientRpc(float speed)
    {
        if (!IsOwner) // Only affect remote clients
        {
            remoteCurrentSpeed = speed;
        }
    }

    [ServerRpc]
    private void StopParticlesServerRpc()
    {
        StopParticlesClientRpc();
    }

    [ClientRpc]
    private void StopParticlesClientRpc()
    {
        if (!IsOwner) // Only affect remote clients
        {
            remoteIsEmitting = false;
            remoteCurrentSpeed = 0f;
            timeSinceLastBurst = 0f;

            if (sprintParticles.isPlaying)
            {
                sprintParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (sprintParticles.isPlaying && sprintParticles.particleCount == 0)
            {
                sprintParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}

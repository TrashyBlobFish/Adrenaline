using System.Globalization;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.SocialPlatforms.Impl;


public class PlayerMovement : NetworkBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public GameObject Cameraholder;
    public GameObject Shield;
    public CameraModeSwitcher cameraSwitcher;
    public PlayerInput playerInput;
    public InputActionAsset inputActions;
    public GameObject GameUI;


    private Rigidbody rb;
    private NetworkObject rootNetworkObject;

    [Header("Audio")]
    [SerializeField] private Transform audioManager;
    [SerializeField] private AudioSource WalkAudioSource;
    [SerializeField] private AudioSource SonicBoomAudioSource;
    [SerializeField] private AudioSource TrainAudioSource;
    private float trainTargetVolume = 0f;
    private float trainInitialVolume = 0.7f;


    [Header("Audio Settings")]
    public float speedReachedCooldown = 2f;
    private float lastSpeedReachedTime = -10f;

    public float trainFadeSpeed = 2f;
    public float trainGracePeriod = 0.1f;
    private float trainGraceTimer = 0f;

    public float speedThreshold = 18f;
    private bool wasAboveThreshold = false;

    [Header("Movement Settings")]
    private bool canControl = true;
    public float runSpeed = 15f;
    public float sprintSpeed = 20f;
    public float acceleration = 20f;
    public float jumpHeight = 2f;
    public float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;
    public float bounceForce = 50f;
    public PlayerStateMachine stateMachine;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    public bool isGrounded;
    private bool jumpInput;
    private bool Launched;
    private NetworkVariable<float> networkBatHoldTime = new NetworkVariable<float>(
    0f,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Owner
);

    public float BatHoldTime => networkBatHoldTime.Value;
    private float batHoldTime = 0f;
    private bool isBatTimerActive = false;
    private NetworkVariable<bool> hasBaseballBat = new NetworkVariable<bool>(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
);
    public bool HasBaseballBat
    {
        get => hasBaseballBat.Value;
        set
        {
            if (IsServer)
                hasBaseballBat.Value = value;
            // The OnValueChanged event will handle enabling/disabling the bat
        }
    }
    public GameObject BaseballBat;
    private RigidbodyConstraints defaultConstraints;

    [Header("Trip Settings")]
    public float tripRange = 2f;
    public LayerMask playerLayerMask;
    public float tripCooldown = 3f;
    [Header("Trip Launch Values")]
    public float tripForwardForce = 15f;
    public float tripUpwardForce = 5f;
    public float tripLaunchDuration = 1f;
    private float lastTripTime = -10f;
    private bool tripInput;

    [Header("Sprint / Stamina Settings")]
    public Slider staminaSlider;
    public float maxStamina = 5f;
    public float minStaminaToActivate = 0.5f; // Required to start
    public float minStaminaToUse = 0.1f;
    public float staminaDrainRate = 1f;
    public float staminaRegenRate = 2f;
    [HideInInspector] public float currentStamina;
    public bool hasStaminaToActivate => currentStamina > minStaminaToActivate;
    public bool hasStaminaToUse => currentStamina > minStaminaToUse;

    [Header("Internal")]
    [HideInInspector] public float movementspeed;
    public bool wallrunning = false;

    // Networking
    private NetworkVariable<bool> shielding = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private UserProfileData userProfile;

    void Awake()
    {
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();
        

        if (playerInput.actions == null)
        {
            var loadedActions = Resources.Load<InputActionAsset>("Player Controls");
            if (loadedActions != null)
            {
                playerInput.actions = loadedActions;
            }
            else if (inputActions != null)
            {
                playerInput.actions = inputActions;
            }
            else
            {
                Debug.LogError("InputActionAsset not found or assigned!");
            }
        }
    }

    void Start()
    {
        if (stateMachine == null)
            stateMachine = GetComponent<PlayerStateMachine>();
        userProfile = Object.FindFirstObjectByType<UserProfileData>();
        if (BaseballBat != null)
            BaseballBat.SetActive(HasBaseballBat);
        rootNetworkObject = GetComponentInParent<NetworkObject>();
        staminaSlider = GameObject.Find("Sprint Slider").GetComponent<Slider>();
        rb = GetComponent<Rigidbody>();
        defaultConstraints = rb.constraints;

        if (playerCamera == null)
            playerCamera = Camera.main;
        if (GameUI == null)
        {
            GameUI = GameObject.Find("Game UI");
            GameUI.SetActive(true);
        }

        // Setup audio sources if not assigned
        if (audioManager == null)
            audioManager = transform.Find("AudioManager");
        if (TrainAudioSource != null)
            trainInitialVolume = TrainAudioSource.volume;


        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentStamina = maxStamina;
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = maxStamina;
        }
    }

    void Update()
    {
        if (!rootNetworkObject.IsOwner)
        {
            playerCamera.gameObject.SetActive(false);
            Cameraholder.SetActive(false);
            return;
        }
        //tracks bat hold time
        if (isBatTimerActive)
        {
            batHoldTime += Time.deltaTime;
            networkBatHoldTime.Value = batHoldTime;
        }
        // Input caching for state machine
        jumpInput = playerInput.actions["Jump"].WasPressedThisFrame();
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        // Trip input (using 'T' key as default - you can change this in the Input Actions asset)
        if (playerInput.actions.FindAction("Trip") != null)
        {
            tripInput = playerInput.actions["Trip"].WasPressedThisFrame();
        }
        else
        {
            // Fallback to keyboard input if Trip action doesn't exist
            tripInput = Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame;
        }

        // Shield input
        bool shieldInput = playerInput.actions["Shield"].IsPressed();

        if (shielding.Value)
        {
            // Shield is currently active, drain stamina
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(0f, currentStamina);

            // Deactivate if stamina is too low or button released
            if (!shieldInput || !hasStaminaToUse)
            {
                shielding.Value = false;
            }
        }
        else
        {
            // Only activate if button pressed and enough stamina to activate
            if (shieldInput && hasStaminaToActivate)
            {
                userProfile.TimesShieldUsed++;
                shielding.Value = true;
            }
        }

        // Handle trip input
        if (tripInput && Time.time - lastTripTime > tripCooldown && isGrounded)
        {
            HandleTrip();
            Debug.Log("Trip input detected");
        }

        //Handles sound
        float currentSpeed = rb.linearVelocity.magnitude;
        bool aboveThreshold = currentSpeed >= speedThreshold;

        // Walk sound effect: play when moving, not sprinting, not above threshold, and grounded
        bool isMoving = rb.linearVelocity.magnitude > 0.1f && isGrounded;
        bool isSprinting = stateMachine != null && stateMachine.currentState is SprintingState;

        if (isMoving && !isSprinting && !aboveThreshold)
        {
            if (WalkAudioSource != null && !WalkAudioSource.isPlaying)
            {
                WalkAudioSource.loop = true;
                WalkAudioSource.Play();
            }
        }
        else
        {
            if (WalkAudioSource != null && WalkAudioSource.isPlaying)
            {
                WalkAudioSource.Stop();
            }
        }

        // Speed reached sound (once, with cooldown after dropping below threshold)
        if (aboveThreshold && !wasAboveThreshold)
        {
            if (Time.time - lastSpeedReachedTime > speedReachedCooldown)
            {
                if (SonicBoomAudioSource != null)
                    SonicBoomAudioSource.Play();
                lastSpeedReachedTime = Time.time;
            }
        }
        if (!aboveThreshold && wasAboveThreshold)
        {
            // Player dropped below threshold, start cooldown timer
            lastSpeedReachedTime = Time.time;
        }

        // Train sound effect: play when above threshold, stop when below
        if (aboveThreshold)
        {
            trainTargetVolume = trainInitialVolume;
            if (!TrainAudioSource.isPlaying)
            {
                TrainAudioSource.volume = 0f; // Start silent
                TrainAudioSource.loop = true;
                TrainAudioSource.Play();
            }
        }
        else
        {
            trainTargetVolume = 0f;
        }

        // Smoothly adjust volume towards target
        TrainAudioSource.volume = Mathf.MoveTowards(
            TrainAudioSource.volume,
            trainTargetVolume,
            trainFadeSpeed/1.5f * Time.deltaTime
        );

        // Only stop after volume has been zero for a grace period
        if (TrainAudioSource.volume <= 0.01f)
        {
            trainGraceTimer += Time.deltaTime;
            if (trainGraceTimer > trainGracePeriod && TrainAudioSource.isPlaying)
            {
                TrainAudioSource.Stop();
                trainGraceTimer = 0f;
            }
        }
        else
        {
            trainGraceTimer = 0f;
        }

        wasAboveThreshold = aboveThreshold;


        // Respawn check
        if (transform.position.y < -50)
        {
            userProfile.NumberOfFalls++;
            transform.position = GameObject.Find("Respawn point").transform.position;
        }

        // UI update
        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;
        }
    }

    private void HandleTrip()
    {
        Debug.Log("=== TRIP ATTEMPT STARTED ===");
        lastTripTime = Time.time;

        Vector3 searchPosition = transform.position + Vector3.up * 0.5f;
        Debug.Log($"Trip search position: {searchPosition}, tripRange: {tripRange}");

        Collider[] nearbyColliders = Physics.OverlapSphere(searchPosition, tripRange);
        Debug.Log($"[COLLISION DETECTION] OverlapSphere found {nearbyColliders.Length} colliders at position {searchPosition}");

        if (nearbyColliders.Length == 0)
        {
            Debug.LogWarning("[COLLISION DETECTION] No colliders detected in trip range!");
            return;
        }

        foreach (Collider collider in nearbyColliders)
        {
            Debug.Log($"[COLLIDER CHECK] Processing collider: {collider.gameObject.name}, Type: {collider.GetType().Name}");

            PlayerMovement targetPlayer = collider.GetComponent<PlayerMovement>();
            Debug.Log($"[PLAYERMOVEMENT SEARCH] GetComponent result: {(targetPlayer != null ? targetPlayer.gameObject.name : "null")}");

            if (targetPlayer == null)
            {
                targetPlayer = collider.GetComponentInParent<PlayerMovement>();
                Debug.Log($"[PLAYERMOVEMENT SEARCH] GetComponentInParent result: {(targetPlayer != null ? targetPlayer.gameObject.name : "null")}");
            }

            if (targetPlayer == null)
            {
                targetPlayer = collider.GetComponentInChildren<PlayerMovement>();
                Debug.Log($"[PLAYERMOVEMENT SEARCH] GetComponentInChildren result: {(targetPlayer != null ? targetPlayer.gameObject.name : "null")}");
            }

            if (targetPlayer == null)
            {
                Debug.Log($"[VALIDATION FAILED] No PlayerMovement component found on {collider.gameObject.name}");
                continue;
            }

            Debug.Log($"[FOUND TARGET] PlayerMovement found: {targetPlayer.gameObject.name}");
            Debug.Log($"[OWNERSHIP CHECK] targetPlayer.IsOwner: {targetPlayer.IsOwner}, this player IsOwner: {IsOwner}");
            Debug.Log($"[SELF CHECK] targetPlayer == this: {targetPlayer == this}");

            if (targetPlayer.IsOwner)
            {
                Debug.Log($"[VALIDATION FAILED] Target {targetPlayer.gameObject.name} is owned by player, skipping");
                continue;
            }

            if (targetPlayer == this)
            {
                Debug.Log($"[VALIDATION FAILED] Target is self, skipping");
                continue;
            }

            // Get direction to target
            Vector3 targetPosition = targetPlayer.transform.position;
            Vector3 sourcePosition = transform.position;

            Debug.Log($"[DIRECTION CALCULATION] Source: {sourcePosition}, Target: {targetPosition}");

            Vector3 directionToTarget = (targetPosition - sourcePosition).normalized;
            Debug.Log($"[TARGET FOUND] Target in range: {targetPlayer.gameObject.name}");

            // Apply trip - no additional checks needed
            Vector3 launchDirection = directionToTarget;
            launchDirection.y = 0;
            launchDirection = launchDirection.normalized;

            Vector3 tripLaunchVector = launchDirection * tripForwardForce + Vector3.up * tripUpwardForce;
            Debug.Log($"[RPC CALL] Calling RequestTripPlayerServerRpc with launchVector: {tripLaunchVector}");
            RequestTripPlayerServerRpc(targetPlayer.NetworkObjectId, tripLaunchVector);
            Debug.Log("[TRIP EXECUTED] Trip successful!");
            break;
        }

        Debug.Log("=== TRIP ATTEMPT ENDED ===");
    }

    //intilizes timer when bat is held
    public void StartBatTimer()
    {
        isBatTimerActive = true;
        Debug.Log($"{name} started bat timer. Current: {batHoldTime}");
    }
    public void StopBatTimer()
    {
        isBatTimerActive = false;
    }

    public void HandleMovement()
    {
        if (!canControl) return;
        Vector2 input = playerInput.actions["Move"].ReadValue<Vector2>();
        Vector3 direction = new Vector3(input.x, 0f, input.y).normalized;

        // Check if sprinting state is active
        bool isSprinting = stateMachine != null && stateMachine.currentState is SprintingState;
        bool isShielding = shielding.Value;

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + playerCamera.transform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);

            if (cameraSwitcher.isThirdPerson)
                transform.rotation = Quaternion.Euler(0f, angle, 0f);

            float speed = isSprinting ? sprintSpeed : runSpeed;
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            Vector3 targetVelocity = moveDir.normalized * speed;
            targetVelocity.y = rb.linearVelocity.y;

            // If sprinting or shielding, set velocity directly (no acceleration)
            if (isSprinting || isShielding)
            {
                rb.linearVelocity = targetVelocity;
            }
            else
            {
                // Smooth acceleration
                rb.linearVelocity = Vector3.MoveTowards(
                    rb.linearVelocity,
                    targetVelocity,
                    acceleration * Time.deltaTime
                );
            }
        }
        else
        {
            userProfile.TimeSpentAFK += Time.deltaTime;
            Vector3 stopVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            rb.linearVelocity = Vector3.MoveTowards(
                rb.linearVelocity,
                stopVelocity,
                acceleration * Time.deltaTime
            );
        }
    }

    // Jump handling (can be turned into JumpingState later)
    public void HandleJump()
    {
        if (!canControl) return;
        if (jumpInput && isGrounded)
        {
            rb.linearVelocity += Vector3.up * jumpHeight;
        }
    }

    public void ApplyFling(Vector3 launchVector)
    {
        Debug.Log("ApplyFling received: " + launchVector);
        if (!IsServer) return;
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(launchVector, ForceMode.Impulse);
        if (!Launched)
            StartCoroutine(LaunchAndTransferBatRoutine());
    }

    public void ApplyTrip(Vector3 launchVector, float duration)
    {
        Debug.Log("ApplyTrip received: " + launchVector);
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(launchVector, ForceMode.Impulse);
        if (!Launched)
            StartCoroutine(TripRoutine(duration));
    }

    private IEnumerator TripRoutine(float duration)
    {
        // Allow free rotation and disable control (match bat launch)
        rb.constraints = RigidbodyConstraints.None;
        canControl = false;
        Launched = true;

        // Add random angular velocity for trip effect (match bat launch)
        rb.angularVelocity = new Vector3(
            Random.Range(-10f, 10f),
            Random.Range(-10f, 10f),
            Random.Range(-10f, 10f)
        );

        yield return new WaitForSeconds(duration);

        // Restore constraints and control (match bat launch)
        rb.constraints = defaultConstraints;
        canControl = true;
        Launched = false;
        rb.angularVelocity = Vector3.zero;
    }

    private IEnumerator LaunchAndTransferBatRoutine()
    {
        // Allow free rotation and disable control
        rb.constraints = RigidbodyConstraints.None;
        canControl = false;

        // Optionally disable the bat collider to prevent re-hit during launch
        if (BaseballBat != null)
        {
            var batCollider = BaseballBat.GetComponent<Collider>();
            if (batCollider != null)
                batCollider.enabled = false;
        }

        // Add random angular velocity for ragdoll effect
        rb.angularVelocity = new Vector3(
            Random.Range(-10f, 10f),
            Random.Range(-10f, 10f),
            Random.Range(-10f, 10f)
        );

        yield return new WaitForSeconds(2f);

        // Restore constraints and control
        rb.constraints = defaultConstraints;
        canControl = true;
        Launched = false;

        // Transfer the bat after launch
        if (IsServer)
        {
            var allPlayers = Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
            foreach (var player in allPlayers)
            {
                if (player.HasBaseballBat)
                    player.HasBaseballBat = false;
            }
            HasBaseballBat = true;
        }

        // Re-enable the bat collider
        if (BaseballBat != null)
        {
            var batCollider = BaseballBat.GetComponent<Collider>();
            if (batCollider != null)
                batCollider.enabled = true;
        }
    }


    // Networking sync
    public override void OnNetworkSpawn()
    {
        shielding.OnValueChanged += OnShieldingChanged;
        Shield.SetActive(shielding.Value);

        hasBaseballBat.OnValueChanged += OnBaseballBatChanged;
        BaseballBat.SetActive(hasBaseballBat.Value);

        // Force Keyboard&Mouse for owner
        if (IsOwner && playerInput != null)
        {
            playerInput.SwitchCurrentControlScheme(
                Keyboard.current, Mouse.current
            );
        }
    }

    public override void OnNetworkDespawn()
    {
        shielding.OnValueChanged -= OnShieldingChanged;
    }

    private void OnShieldingChanged(bool oldValue, bool newValue)
    {
        Shield.SetActive(newValue);
    }

    private void OnBaseballBatChanged(bool oldValue, bool newValue)
    {
        if (BaseballBat != null)
            BaseballBat.SetActive(newValue);

        if (newValue)
            StartBatTimer();
        else
            StopBatTimer();
    }

    [ServerRpc]
    public void RequestFlingPlayerServerRpc(ulong targetId, Vector3 launchVector)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out var netObj))
        {
            var target = netObj.GetComponentInChildren<PlayerMovement>();
            Debug.Log(target);
            if (target != null)
            {
                Debug.Log("ApplyFling called on: " + target.name);
                target.ApplyFlingClientRpc(launchVector);
            }
        }
    }

    [ServerRpc]
    public void RequestTripPlayerServerRpc(ulong targetId, Vector3 launchVector)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out var netObj))
        {
            var target = netObj.GetComponentInChildren<PlayerMovement>();
            if (target != null)
            {
                Debug.Log("ApplyTrip called on: " + target.name);
                target.ApplyTripClientRpc(launchVector, tripLaunchDuration);
            }
        }
    }

    [ClientRpc]
    public void ApplyFlingClientRpc(Vector3 launchVector)
    {
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(launchVector, ForceMode.Impulse);
        if (!Launched)
            StartCoroutine(LaunchAndTransferBatRoutine());
    }

    [ClientRpc]
    public void ApplyTripClientRpc(Vector3 launchVector, float duration)
    {
        ApplyTrip(launchVector, duration);
    }

    // Visualization for trip range in editor
    private void OnDrawGizmosSelected()
    {
        // Draw trip range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, tripRange);
        
        // Draw forward direction cone
        Gizmos.color = Color.yellow;
        Vector3 forward = transform.forward * tripRange;
        Vector3 left = Quaternion.Euler(0, -30, 0) * forward;
        Vector3 right = Quaternion.Euler(0, 30, 0) * forward;
        
        Gizmos.DrawLine(transform.position, transform.position + left);
        Gizmos.DrawLine(transform.position, transform.position + right);
        Gizmos.DrawLine(transform.position + left, transform.position + right);
    }
}
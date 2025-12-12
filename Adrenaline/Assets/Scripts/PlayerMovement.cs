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


    private Rigidbody rb;
    private NetworkObject rootNetworkObject;

    [Header("Movement Settings")]
    private bool canControl = true;
    public float runSpeed = 15f;
    public float sprintSpeed = 20f;
    public float jumpHeight = 2f;
    public float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;
    public float bounceForce = 50f;

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
        userProfile = Object.FindFirstObjectByType<UserProfileData>();
        if (BaseballBat != null)
            BaseballBat.SetActive(HasBaseballBat);
        rootNetworkObject = GetComponentInParent<NetworkObject>();
        staminaSlider = GameObject.Find("Sprint Slider").GetComponent<Slider>();
        rb = GetComponent<Rigidbody>();
        defaultConstraints = rb.constraints;

        if (playerCamera == null)
            playerCamera = Camera.main;

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

        // Shield input
        bool shieldInput = playerInput.actions["Shield"].IsPressed();
        if (shieldInput && hasStaminaToActivate)
        {
            userProfile.TimesShieldUsed++;
            shielding.Value = true;
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(0f, currentStamina);
        }
        else
        {
            shielding.Value = false;
        }



        // Respawn check
        if (transform.position.y < -100)
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
        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + playerCamera.transform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);

            if (cameraSwitcher.isThirdPerson)
                transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            Vector3 newVelocity = moveDir.normalized * movementspeed;
            newVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = newVelocity;
        }
        else
        {
            userProfile.TimeSpentAFK += Time.deltaTime;
            // Stop horizontal movement if idle
            if (!Launched)
            {
                Vector3 stopVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                rb.linearVelocity = stopVelocity;
            }
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

    // Bat launch knockback
    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("BaseballBat"))
        {
            if (!collision.transform.IsChildOf(transform))
            {
                Launched = true;
                StartCoroutine(LaunchAndTransferBatRoutine());

                Vector3 forward = collision.transform.forward;
                Vector3 up = Vector3.up;
                float forwardForce = 40f;
                float upwardForce = 10f;

                Vector3 launchVector = (forward * forwardForce) + (up * upwardForce);
                rb.linearVelocity = Vector3.zero;
                rb.AddForce(launchVector, ForceMode.Impulse);
            }
        }
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
}
using System.Globalization;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.Rendering;


public class PlayerMovement : NetworkBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public GameObject Cameraholder;
    public GameObject Shield;
    public CameraModeSwitcher cameraSwitcher;
    public PlayerInput playerInput;

    private Rigidbody rb;
    private NetworkObject rootNetworkObject;

    [Header("Movement Settings")]
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

    [Header("Sprint / Stamina Settings")]
    public Slider staminaSlider;
    public float maxStamina = 5f;
    public float staminaDrainRate = 1f;
    public float staminaRegenRate = 2f;
    [HideInInspector] public float currentStamina;
    public bool hasStam => currentStamina > 0.1f;

    [Header("Internal")]
    [HideInInspector] public float movementspeed;

    // Networking
    private NetworkVariable<bool> shielding = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    void Start()
    {
        rootNetworkObject = GetComponentInParent<NetworkObject>();
        staminaSlider = GameObject.Find("Sprint Slider").GetComponent<Slider>();
        rb = GetComponent<Rigidbody>();

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

        // Input caching for state machine
        jumpInput = playerInput.actions["Jump"].WasPressedThisFrame();
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        // Shield input
        bool shieldInput = playerInput.actions["Shield"].IsPressed();
        if (shieldInput && hasStam)
        {
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
            transform.position = GameObject.Find("Respawn point").transform.position;
        }

        // UI update
        if (staminaSlider != null)
            staminaSlider.value = currentStamina;
    }

    public void HandleMovement()
    {
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
        if (jumpInput && isGrounded)
        {
            rb.linearVelocity += Vector3.up * jumpHeight;
        }
    }

    // Shield launch knockback
    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Shield"))
        {
            if (!collision.transform.IsChildOf(transform))
            {
                Launched = true;
                StartCoroutine(ResetLaunch());

                Vector3 forward = collision.transform.forward;
                Vector3 up = Vector3.up;
                float forwardForce = 20f;
                float upwardForce = 10f;

                Vector3 launchVector = (forward * forwardForce) + (up * upwardForce);
                rb.linearVelocity = Vector3.zero;
                rb.AddForce(launchVector, ForceMode.Impulse);
            }
        }
    }

    private IEnumerator ResetLaunch()
    {
        yield return new WaitForSeconds(2f);
        Launched = false;
    }

    // Networking sync
    public override void OnNetworkSpawn()
    {
        shielding.OnValueChanged += OnShieldingChanged;
        Shield.SetActive(shielding.Value);
    }

    public override void OnNetworkDespawn()
    {
        shielding.OnValueChanged -= OnShieldingChanged;
    }

    private void OnShieldingChanged(bool oldValue, bool newValue)
    {
        Shield.SetActive(newValue);
    }
}
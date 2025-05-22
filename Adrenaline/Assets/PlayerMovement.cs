using System.Globalization;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.Rendering;


public class PlayerMovement : NetworkBehaviour
{
    public Camera playerCamera;
    public GameObject Cameraholder;
    public GameObject Shield;
    public CameraModeSwitcher cameraSwitcher;

    private Rigidbody rb;

    public float runSpeed = 15f;
    public float sprintSpeed = 20f;
    public float jumpHeight = 2;
    public float turnSmoothTime = 0.1f;
    public float bounceForce = 50f;
    public InputActionMap PlayerActionMap;
    public PlayerInput playerInput;

    private bool jumpInput;

    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    public bool isGrounded;
    private float movementspeed;
    private bool Launched;

    [Header("Sprint/Stamina Settings")]
    public Slider staminaSlider; // assign this in the Inspector
    public float maxStamina = 5f;
    public float staminaDrainRate = 1f;  // stamina units drained per second while sprinting
    public float staminaRegenRate = 2f;  // stamina units regenerated per second while not sprinting

    private float currentStamina;
    public bool hasStam => currentStamina > 0.1f;

    private NetworkVariable<bool> shielding = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    float turnSmoothVelocity;

    void Start()
    {
        staminaSlider = GameObject.Find("Sprint Slider").GetComponent<Slider>();
        playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody>();
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentStamina = maxStamina; // Start full
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = maxStamina;
        }
    }
 

    void Update()
    {
        if (!IsOwner)
        {
            playerCamera.gameObject.SetActive(false);
            Cameraholder.SetActive(false);
            return;
        }
        if (IsOwner)
        {
            bool shieldInput = playerInput.actions["Shield"].IsPressed();

            if (shieldInput && hasStam)
            {
                shielding.Value = true;
                currentStamina -= staminaDrainRate * Time.deltaTime;
                if (currentStamina < 0f)
                {
                    currentStamina = 0f;
                }
            }
            else
            {
                shielding.Value = false;
            }
        }
        

        // Calculate movement direction
        Vector2 input = playerInput.actions["Move"].ReadValue<Vector2>();
        jumpInput = playerInput.actions["Jump"].WasPressedThisFrame();
        
        Vector3 direction = new Vector3(input.x, 0f, input.y).normalized;

        //is grounded check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded)
        {
            Launched = false;
        }
        if (jumpInput && isGrounded)
        {
            Debug.Log("jump attempted");
            rb.linearVelocity += Vector3.up * jumpHeight;
        }
        //Manages sprint and the stamina bar
        if (playerInput.actions["Sprint"].IsPressed() && hasStam)
        {
            movementspeed = sprintSpeed;
            currentStamina -= staminaDrainRate * Time.deltaTime;
            if (currentStamina < 0f)
            {
                currentStamina = 0f;
                
            }
        }
        else if (!playerInput.actions["Sprint"].IsPressed())
        {
            movementspeed = runSpeed;
            currentStamina += staminaRegenRate * Time.deltaTime;
            if (currentStamina > maxStamina)
            {
                currentStamina = maxStamina;
            }
        }

        

        //Death Plane
        if (transform.position.y < -100)
        {
            transform.position = GameObject.Find("Respawn point").transform.position;
        }




        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + playerCamera.transform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            if (cameraSwitcher.isThirdPerson)
            {
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }


            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                
                Vector3 newVelocity = moveDir.normalized * movementspeed;
                newVelocity.y = rb.linearVelocity.y;
                rb.linearVelocity = newVelocity;
        }
        else
        {
            // Stop horizontal movement, keep gravity
            if (!Launched)
            {
                Vector3 stopVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                rb.linearVelocity = stopVelocity;
            }
            
        }
        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;
        }

    }
    void OnCollisionEnter(Collision collision)
    {
        // Check if collided object has tag "Shield"
        if (collision.collider.CompareTag("Shield"))
        {
            // Check if the shield is NOT a child of this object
            if (!collision.transform.IsChildOf(transform))
            {
                Launched = true;
                float launchForce = 100f;
                float uplaunchForce = 20f;
                StartCoroutine(ResetLaunch());


                Vector3 upLaunchDirection = (Vector3.up * 0.3f).normalized;
                Vector3 launchDirection = (collision.transform.forward).normalized;
                rb.AddForce((launchDirection * launchForce) + (upLaunchDirection * uplaunchForce) , ForceMode.Impulse);

            }
        }
    }
    private IEnumerator ResetLaunch()
    {
        yield return new WaitForSeconds(2f);
        Launched = false;
    }
    public override void OnNetworkSpawn()
    {
        shielding.OnValueChanged += OnShieldingChanged;
        Shield.SetActive(shielding.Value); // Sync initial state
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
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    public Camera playerCamera;
    public GameObject Cameraholder;
    public GameObject Shield;
    public CameraModeSwitcher cameraSwitcher;

    private Rigidbody rb;

    public float speed = 15f;
    public float jumpHeight = 2;
    public float turnSmoothTime = 0.1f;
    public float bounceForce = 50f;
    public InputActionMap PlayerActionMap;
    public PlayerInput playerInput;

    private bool jumpInput;

    float turnSmoothVelocity;

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody>();
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
 

    void Update()
    {
        if (!IsOwner)
        {
            playerCamera.gameObject.SetActive(false);
            Cameraholder.SetActive(false);
            return;
        }
        // Get input for movement
        //horizontalInput = Input.GetAxisRaw("Horizontal");
        //verticalInput = Input.GetAxisRaw("Vertical");

        // Calculate movement direction
        Vector2 input = playerInput.actions["Move"].ReadValue<Vector2>();
        jumpInput = playerInput.actions["Jump"].WasPressedThisFrame();
        bool shieldInput = playerInput.actions["Shield"].IsPressed();
        Vector3 direction = new Vector3(input.x, 0f, input.y).normalized;
        if (jumpInput)
        {
            Debug.Log("jump attempted");
            rb.linearVelocity += Vector3.up * jumpHeight;
        }
        if (shieldInput)
        {
            Shield.SetActive(true);
        }
        else
        {
            Shield.SetActive(false);
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
            Vector3 newVelocity = moveDir.normalized * speed;
            newVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = newVelocity;
        }
        else
        {
            // Stop horizontal movement, keep gravity
            Vector3 stopVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            rb.linearVelocity = stopVelocity;
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
                rb.linearVelocity += Vector3.up * bounceForce;
            }
        }
    }
}
using Unity.VisualScripting;
using UnityEngine;

public class ButtonActivator : MonoBehaviour
{
    [Header("Buttons to Check")]
    public OneWayButton[] buttons;   // Assign all your button GameObjects here

    [Header("Target Object")]
    public Rigidbody targetRigidbody;  // The object whose Rigidbody you want to enable

    public GameObject Boss;

    private bool activated = false;

    void Start()
    {
        
    }

    void Update()
    {
        if (activated) return; // already triggered

        if (Boss != null)
        {
            Vector3 bossPos = Boss.transform.position;
            float yOffset = 30f; // Change this value as needed
            gameObject.transform.position = new Vector3(bossPos.x, bossPos.y + yOffset, bossPos.z);
        }
        // Check if all buttons are pressed
        bool allPressed = true;
        foreach (OneWayButton button in buttons)
        {
            if (button == null || !IsButtonPressed(button))
            {
                allPressed = false;
                break;
            }
        }

        // If all buttons are pressed, enable Rigidbody
        if (allPressed)
        {
            ActivateObject();
        }
    }

    private bool IsButtonPressed(OneWayButton button)
    {
        // Access the internal state (make isPushed public or add a getter)
        return button.IsPressed();
    }

    private void ActivateObject()
    {
        activated = true;
        Debug.Log("All buttons pressed! Activating object.");

        Boss.GetComponent<TutorialBoss>().SetPhase(3);

        // Reset ALL buttons after Phase 3
        foreach (OneWayButton button in buttons)
        {
            if (button != null)
                button.ResetButton();
        }

        if (targetRigidbody != null)
        {
            targetRigidbody.isKinematic = false;
            targetRigidbody.useGravity = true;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == Boss)
        {
            DisableObject();
        }
    }
    private void DisableObject()
    {
        // Disable mesh
        MeshRenderer mesh = GetComponent<MeshRenderer>();
        if (mesh != null)
            mesh.enabled = false;

        // Disable collider
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        // Disable rigidbody influence
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;  // No physics influence
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log("Object visually hidden and physics disabled.");
    }
    public void EnableObject()
    {
        // Enable mesh
        MeshRenderer mesh = GetComponent<MeshRenderer>();
        if (mesh != null)
            mesh.enabled = true;

        // Enable colliders (supports multiple)
        Collider[] cols = GetComponents<Collider>();
        foreach (var col in cols)
            col.enabled = true;

        // Enable Rigidbody physics again
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        Debug.Log($"{name} enabled (visible + physics active).");
    }

}

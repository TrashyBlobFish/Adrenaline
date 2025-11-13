using UnityEngine;

public class OneWayButton : MonoBehaviour
{
    public Material notPushedMaterial;  // Material before the button is pressed
    public Material pushedMaterial;     // Material after the button is pressed

    private Renderer rend;
    private bool isPushed = false;

    void Start()
    {
        rend = GetComponent<Renderer>();

        // Set initial material
        if (rend != null && notPushedMaterial != null)
            rend.material = notPushedMaterial;
    }

    // Call when the button is pressed
    public void PressButton()
    {
        if (isPushed) return; // Already pressed — do nothing

        isPushed = true;

        if (rend != null && pushedMaterial != null)
            rend.material = pushedMaterial;

        Debug.Log("Button pressed and locked on!");
    }

    // Optional: automatically trigger when something collides
    void OnCollisionEnter(Collision collision)
    {
        if (!isPushed && collision.collider.CompareTag("Player"))
        {
            PressButton();
        }
    }
    public bool IsPressed()
    {
        return isPushed;
    }
}
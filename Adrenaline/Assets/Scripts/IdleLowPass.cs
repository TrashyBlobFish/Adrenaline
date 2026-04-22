using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(Rigidbody))]
public class IdleLowPass : NetworkBehaviour
{
    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string exposedParameterName = "AmbienceLowPass";

    [Header("Cutoff")]
    [SerializeField] private float movingCutoff = 22000f;
    [SerializeField] private float idleCutoff = 500f;
    [SerializeField] private float idleDelay = 4f;
    [SerializeField] private float cutoffChangeSpeed = 8000f;
    [SerializeField] private float moveThreshold = 0.1f;

    private Rigidbody rb;
    private float idleTimer;
    private float currentCutoff;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        currentCutoff = movingCutoff;
    }

    private void OnEnable()
    {
        if (mixer != null)
            mixer.SetFloat(exposedParameterName, movingCutoff);
    }

    private void Update()
    {
        if (!IsOwner || mixer == null || rb == null)
            return;

        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        bool isMoving = horizontalVelocity.sqrMagnitude > moveThreshold * moveThreshold;

        if (isMoving)
            idleTimer = 0f;
        else
            idleTimer += Time.deltaTime;

        float targetCutoff = idleTimer >= idleDelay ? idleCutoff : movingCutoff;
        currentCutoff = Mathf.MoveTowards(currentCutoff, targetCutoff, cutoffChangeSpeed * Time.deltaTime);

        mixer.SetFloat(exposedParameterName, currentCutoff);
    }

    private void OnDisable()
    {
        if (mixer != null)
            mixer.SetFloat(exposedParameterName, movingCutoff);
    }
}
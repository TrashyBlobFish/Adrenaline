using UnityEngine;
using UnityEngine.Audio;

public class SnapshotZone : MonoBehaviour
{
    public AudioMixerSnapshot inside;
    public AudioMixerSnapshot outside;
    public float transitionTime = 1f;

    void OnTriggerEnter(Collider other)
    {
        inside.TransitionTo(transitionTime);
    }

    void OnTriggerExit(Collider other)
    {
        outside.TransitionTo(transitionTime);
    }
}

using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class ProximityAudioTrigger : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform playerRoot;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private bool hasPlayed;

    private void Awake()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        box.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasPlayed)
            return;

        if (!IsPlayer(other))
            return;

        // Set this BEFORE Play() so another player collider
        // entering on the same frame cannot trigger it again.
        hasPlayed = true;

        if (audioSource != null)
            audioSource.Play();
    }

    private bool IsPlayer(Collider other)
    {
        if (playerRoot == null || other == null)
            return false;

        Transform otherTransform = other.transform;

        return otherTransform == playerRoot ||
               otherTransform.IsChildOf(playerRoot) ||
               playerRoot.IsChildOf(otherTransform);
    }
}
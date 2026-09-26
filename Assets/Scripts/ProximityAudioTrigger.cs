using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class ProximityAudioTrigger : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform playerRoot;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("If enabled, the sound stops when the player leaves the area.")]
    [SerializeField] private bool stopOnExit = true;

    private readonly HashSet<Collider> playerColliders = new();

    private void Awake()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        box.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        if (audioSource != null)
            audioSource.playOnAwake = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerColliders.Add(other);

        // Only trigger once when the first part of the player enters.
        if (playerColliders.Count == 1 &&
            audioSource != null &&
            !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerColliders.Remove(other);

        if (playerColliders.Count == 0 &&
            stopOnExit &&
            audioSource != null)
        {
            audioSource.Stop();
        }
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

using UnityEngine;
using UnityEngine.Video;

public class PostProductionExhibitController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform playerRoot;

    [Header("Video")]
    [SerializeField] private VideoPlayer hallwayVideo;

    [Tooltip("Restart the comparison video from the beginning each time the player enters.")]
    [SerializeField] private bool restartOnEnter = true;

    [Tooltip("Pause the video when the player leaves the exhibit.")]
    [SerializeField] private bool pauseOnExit = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private int playerColliderCount;

    private void Start()
    {
        if (hallwayVideo != null)
        {
            // Decode/prepare the video ahead of time so entering
            // the exhibit does not cause as much startup delay.
            hallwayVideo.Prepare();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other))
            return;

        playerColliderCount++;

        // Only start once when the first part of the VR rig enters.
        if (playerColliderCount > 1)
            return;

        StartExhibit();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other))
            return;

        playerColliderCount =
            Mathf.Max(0, playerColliderCount - 1);

        // The Meta rig may contain multiple colliders.
        // Don't stop until all of them have left the trigger.
        if (playerColliderCount > 0)
            return;

        StopExhibit();
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (playerRoot == null || other == null)
            return false;

        Transform otherTransform = other.transform;

        return otherTransform == playerRoot ||
               otherTransform.IsChildOf(playerRoot);
    }

    private void StartExhibit()
    {
        if (hallwayVideo == null)
        {
            Debug.LogWarning(
                "Post Production exhibit has no VideoPlayer assigned.",
                this
            );

            return;
        }

        if (restartOnEnter)
        {
            hallwayVideo.Stop();
            hallwayVideo.time = 0;
        }

        hallwayVideo.Play();

        if (debugLogging)
            Debug.Log("Post Production exhibit started.", this);
    }

    private void StopExhibit()
    {
        if (hallwayVideo == null)
            return;

        if (pauseOnExit)
            hallwayVideo.Pause();

        if (debugLogging)
            Debug.Log("Post Production exhibit paused.", this);
    }
}

using System.Collections;
using UnityEngine;
using Oculus.Interaction;

public class WearableHeadphones : MonoBehaviour
{
    [Header("Grab")]
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private Rigidbody headphoneRigidbody;

    [Header("Wear Detection")]
    [SerializeField] private Transform wearAnchor;

    [Tooltip("Point inside the headphones where the player's head should be.")]
    [SerializeField] private Transform wearCheckPoint;

    [SerializeField] private float snapDistance = 0.40f;

    [Header("Audio")]
    [SerializeField] private AudioSource playerAmbientAudio;
    [SerializeField] private AudioSource headphoneAudio;

    [SerializeField] private bool restartSongWhenWorn = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    public bool IsWorn { get; private set; }

    private bool ambientWasMuted;
    private Coroutine releaseRoutine;

    private void OnEnable()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised += HandlePointerEvent;
    }

    private void OnDisable()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised -= HandlePointerEvent;

        RestoreNormalAudio();
    }

    private void HandlePointerEvent(PointerEvent evt)
    {
        switch (evt.Type)
        {
            case PointerEventType.Select:

                // Grabbing them while worn means the player
                // is taking them back off.
                if (IsWorn)
                    BeginTakeOff();

                break;

            case PointerEventType.Unselect:

                if (releaseRoutine != null)
                    StopCoroutine(releaseRoutine);

                // IMPORTANT:
                // Test the distance NOW, before physics throws/moves
                // the headphones on the next frame.
                float distance = GetWearDistance();
                bool shouldWear = distance <= snapDistance;

                if (debugLogging)
                {
                    Debug.Log(
                        $"Headphones released {distance:F3}m from head. " +
                        $"Snap distance = {snapDistance:F3}m. " +
                        $"Will wear = {shouldWear}",
                        this
                    );
                }

                releaseRoutine =
                    StartCoroutine(FinishRelease(shouldWear));

                break;
        }
    }

    private IEnumerator FinishRelease(bool shouldWear)
    {
        // Allow Interaction SDK to finish releasing ownership.
        yield return null;

        releaseRoutine = null;

        if (shouldWear)
            PutOn();
        else
            MakePhysical();
    }

    private float GetWearDistance()
    {
        if (wearAnchor == null)
            return float.MaxValue;

        Transform checkPoint =
            wearCheckPoint != null
                ? wearCheckPoint
                : transform;

        return Vector3.Distance(
            checkPoint.position,
            wearAnchor.position
        );
    }

    private void PutOn()
    {
        if (IsWorn || wearAnchor == null)
            return;

        IsWorn = true;

        if (headphoneRigidbody != null)
        {
            headphoneRigidbody.linearVelocity =
                Vector3.zero;

            headphoneRigidbody.angularVelocity =
                Vector3.zero;

            headphoneRigidbody.useGravity = false;
            headphoneRigidbody.isKinematic = true;
        }

        // Parent the complete headphone object to the head anchor.
        transform.SetParent(wearAnchor, false);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        StartHeadphoneAudio();

        if (debugLogging)
            Debug.Log("Headphones equipped.", this);
    }

    private void BeginTakeOff()
    {
        IsWorn = false;

        // Preserve world position when grabbing them off the head.
        transform.SetParent(null, true);

        StopHeadphoneAudio();

        if (debugLogging)
            Debug.Log("Headphones removed.", this);
    }

    private void MakePhysical()
    {
        IsWorn = false;

        transform.SetParent(null, true);

        if (headphoneRigidbody != null)
        {
            headphoneRigidbody.useGravity = true;
            headphoneRigidbody.isKinematic = false;
        }

        StopHeadphoneAudio();
    }

    private void StartHeadphoneAudio()
    {
        if (playerAmbientAudio != null)
        {
            ambientWasMuted = playerAmbientAudio.mute;
            playerAmbientAudio.mute = true;
        }

        if (headphoneAudio == null)
            return;

        if (restartSongWhenWorn)
        {
            headphoneAudio.Stop();
            headphoneAudio.time = 0f;
        }

        headphoneAudio.Play();
    }

    private void StopHeadphoneAudio()
    {
        if (headphoneAudio != null)
            headphoneAudio.Stop();

        if (playerAmbientAudio != null)
            playerAmbientAudio.mute = ambientWasMuted;
    }

    private void RestoreNormalAudio()
    {
        if (headphoneAudio != null)
            headphoneAudio.Stop();

        if (playerAmbientAudio != null)
            playerAmbientAudio.mute = ambientWasMuted;
    }
}

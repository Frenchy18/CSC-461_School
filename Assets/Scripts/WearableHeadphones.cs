using System.Collections;
using UnityEngine;
using Oculus.Interaction;

public class WearableHeadphones : MonoBehaviour
{
    [Header("Grab")]
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private Rigidbody headphoneRigidbody;

    [Header("Wear")]
    [SerializeField] private Transform wearAnchor;
    [SerializeField] private float snapDistance = 0.25f;

    [Header("Audio")]
    [SerializeField] private AudioSource playerAmbientAudio;
    [SerializeField] private AudioSource headphoneAudio;

    [SerializeField] private bool restartSongWhenWorn = true;

    public bool IsWorn { get; private set; }

    private bool isGrabbed;
    private bool ambientWasMuted;

    private Coroutine releaseRoutine;

    private void OnEnable()
    {
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised += HandlePointerEvent;
        }
    }

    private void OnDisable()
    {
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised -= HandlePointerEvent;
        }

        RestoreNormalAudio();
    }

    private void HandlePointerEvent(PointerEvent evt)
    {
        switch (evt.Type)
        {
            case PointerEventType.Select:

                isGrabbed = true;

                // Player grabbed the headphones while they were being worn.
                if (IsWorn)
                {
                    BeginTakeOff();
                }

                break;


            case PointerEventType.Unselect:

                isGrabbed = false;

                if (releaseRoutine != null)
                    StopCoroutine(releaseRoutine);

                releaseRoutine = StartCoroutine(FinishRelease());

                break;


            case PointerEventType.Cancel:

                isGrabbed = false;

                break;
        }
    }

    private IEnumerator FinishRelease()
    {
        // Let Interaction SDK completely finish its release first.
        yield return null;

        releaseRoutine = null;

        if (wearAnchor == null)
            yield break;

        float distance = Vector3.Distance(
            GetHeadphonePosition(),
            wearAnchor.position
        );

        if (distance <= snapDistance)
        {
            PutOn();
        }
        else
        {
            MakePhysical();
        }
    }

    private Vector3 GetHeadphonePosition()
    {
        if (headphoneRigidbody != null)
            return headphoneRigidbody.worldCenterOfMass;

        return transform.position;
    }

    private void PutOn()
    {
        if (IsWorn)
            return;

        IsWorn = true;

        if (headphoneRigidbody != null)
        {
            headphoneRigidbody.linearVelocity = Vector3.zero;
            headphoneRigidbody.angularVelocity = Vector3.zero;

            headphoneRigidbody.useGravity = false;
            headphoneRigidbody.isKinematic = true;
        }

        transform.SetParent(wearAnchor, false);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        StartHeadphoneAudio();
    }

    private void BeginTakeOff()
    {
        IsWorn = false;

        // Preserve world position when removing them from the head.
        transform.SetParent(null, true);

        StopHeadphoneAudio();

        // Don't change Rigidbody state here.
        // Interaction SDK currently owns it while grabbed.
    }

    private void MakePhysical()
    {
        IsWorn = false;

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

        if (!headphoneAudio.isPlaying)
        {
            headphoneAudio.Play();
        }
    }

    private void StopHeadphoneAudio()
    {
        if (headphoneAudio != null)
        {
            headphoneAudio.Stop();
        }

        if (playerAmbientAudio != null)
        {
            playerAmbientAudio.mute = ambientWasMuted;
        }
    }

    private void RestoreNormalAudio()
    {
        if (headphoneAudio != null)
        {
            headphoneAudio.Stop();
        }

        if (playerAmbientAudio != null)
        {
            playerAmbientAudio.mute = ambientWasMuted;
        }
    }
}

using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BunMovementBreathing : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Usually found automatically from Bun's parent object.")]
    [SerializeField] private BunStalker stalker;

    [Tooltip("Usually the AudioSource on this Breathing object.")]
    [SerializeField] private AudioSource breathingSource;

    [Header("Playback")]
    [Tooltip("Pause while Bun is frozen, then resume from the same point when she moves again. This avoids restarting the breath loop every time the player looks away.")]
    [SerializeField] private bool resumeFromPausedPosition = true;

    private bool audioHasStarted;
    private bool lastMovingState;

    private void Awake()
    {
        if (stalker == null)
            stalker = GetComponentInParent<BunStalker>();

        if (breathingSource == null)
            breathingSource = GetComponent<AudioSource>();

        if (breathingSource != null)
        {
            breathingSource.playOnAwake = false;
            breathingSource.loop = true;
            breathingSource.Stop();
        }
    }

    private void Update()
    {
        if (stalker == null || breathingSource == null)
            return;

        bool shouldBeBreathing = stalker.IsMoving;

        if (shouldBeBreathing == lastMovingState)
            return;

        lastMovingState = shouldBeBreathing;

        if (shouldBeBreathing)
        {
            if (resumeFromPausedPosition && audioHasStarted)
            {
                breathingSource.UnPause();
            }
            else
            {
                breathingSource.Play();
                audioHasStarted = true;
            }
        }
        else if (audioHasStarted)
        {
            if (resumeFromPausedPosition)
            {
                breathingSource.Pause();
            }
            else
            {
                breathingSource.Stop();
                audioHasStarted = false;
            }
        }
    }

    private void OnDisable()
    {
        if (breathingSource != null)
            breathingSource.Stop();

        audioHasStarted = false;
        lastMovingState = false;
    }
}
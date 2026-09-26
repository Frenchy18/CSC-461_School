using System.Collections;
using UnityEngine;

public class FinalRevealController : MonoBehaviour
{
    [Header("Progress")]
    [SerializeField] private ExhibitVisitTracker tracker;

    [Header("Player")]
    [SerializeField] private Transform playerHead;

    [Header("Turn-Away Detection")]
    [Tooltip(
        "Degrees the player must turn from the direction they were facing " +
        "when the museum reached 5/5."
    )]
    [Range(90f, 180f)]
    [SerializeField] private float turnedAwayAngle = 130f;

    [Tooltip(
        "Small delay after reaching 5/5 before we begin checking rotation."
    )]
    [SerializeField] private float armDelay = 0.5f;

    [Header("Reveal Objects")]
    [SerializeField] private GameObject chicaRoot;
    [SerializeField] private Light[] revealSpotlights;

    [Header("Museum Lights To Disable")]
    [SerializeField] private Light[] lightsToTurnOff;

    [Header("Reveal Timing")]
    [SerializeField] private float blackoutDuration = 0.35f;

    [Header("Optional Audio")]
    [SerializeField] private AudioSource revealSting;

    private bool museumCompleted;
    private bool revealArmed;
    private bool revealTriggered;

    private Vector3 completionFacingDirection;

    private void OnEnable()
    {
        if (tracker != null)
            tracker.OnMuseumCompleted += HandleMuseumCompleted;
    }

    private void OnDisable()
    {
        if (tracker != null)
            tracker.OnMuseumCompleted -= HandleMuseumCompleted;
    }

    private void Start()
    {
        if (chicaRoot != null)
            chicaRoot.SetActive(false);

        if (revealSpotlights != null)
        {
            foreach (Light light in revealSpotlights)
            {
                if (light != null)
                    light.enabled = false;
            }
        }

        // Mostly useful for testing if completion already happened.
        if (tracker != null && tracker.IsComplete)
        {
            HandleMuseumCompleted();
        }
    }

    private void Update()
    {
        if (!museumCompleted ||
            !revealArmed ||
            revealTriggered ||
            playerHead == null)
        {
            return;
        }

        Vector3 currentFacing = GetHorizontalFacingDirection();

        if (currentFacing.sqrMagnitude < 0.001f ||
            completionFacingDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        float angle = Vector3.Angle(
            completionFacingDirection,
            currentFacing
        );

        if (angle >= turnedAwayAngle)
        {
            revealTriggered = true;
            StartCoroutine(RevealSequence());
        }
    }

    private void HandleMuseumCompleted()
    {
        if (museumCompleted)
            return;

        museumCompleted = true;

        // SNAPSHOT:
        // Remember the direction the player was facing at exactly 5/5.
        completionFacingDirection =
            GetHorizontalFacingDirection();

        StartCoroutine(ArmRevealAfterDelay());
    }

    private IEnumerator ArmRevealAfterDelay()
    {
        yield return new WaitForSeconds(armDelay);

        revealArmed = true;
    }

    private Vector3 GetHorizontalFacingDirection()
    {
        if (playerHead == null)
            return Vector3.zero;

        Vector3 horizontalForward = Vector3.ProjectOnPlane(
            playerHead.forward,
            Vector3.up
        );

        if (horizontalForward.sqrMagnitude < 0.001f)
            return Vector3.zero;

        return horizontalForward.normalized;
    }

    private IEnumerator RevealSequence()
    {
        // GLOBAL BLACKOUT.
        //
        // Disable the Light GameObjects themselves so that
        // exhibit scripts cannot simply re-enable their Light components.
        if (lightsToTurnOff != null)
        {
            foreach (Light light in lightsToTurnOff)
            {
                if (light != null)
                    light.gameObject.SetActive(false);
            }
        }

        // Put Chica into position while the room is dark.
        if (chicaRoot != null)
            chicaRoot.SetActive(true);

        if (blackoutDuration > 0f)
            yield return new WaitForSeconds(blackoutDuration);

        // Reveal Chica.
        if (revealSpotlights != null)
        {
            foreach (Light light in revealSpotlights)
            {
                if (light == null)
                    continue;

                light.gameObject.SetActive(true);
                light.enabled = true;
            }
        }

        if (revealSting != null)
        {
            revealSting.enabled = true;
            revealSting.Play();
        }
    }
}

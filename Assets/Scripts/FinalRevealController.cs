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

    [Header("Reveal Orientation")]
    [SerializeField] private Transform revealPivot;

    [SerializeField] private Transform facingOffset;

    [Tooltip("Corrects Chica's imported forward direction.")]
    [SerializeField] private float chicaYawOffset = 0f;

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
        // Black out the museum.
        if (lightsToTurnOff != null)
        {
            foreach (Light light in lightsToTurnOff)
            {
                if (light != null)
                    light.gameObject.SetActive(false);
            }
        }
        
        // Rotate the complete Chica/light/cupcake rig
        // toward the player's current position while everything is dark.
        FaceRevealTowardPlayer();

        // Now place Chica into the scene.
        if (chicaRoot != null)
            chicaRoot.SetActive(true);

        if (blackoutDuration > 0f)
            yield return new WaitForSeconds(blackoutDuration);

        // Turn on the three reveal spotlights.
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

    private void FaceRevealTowardPlayer()
    {
        if (revealPivot == null || playerHead == null || chicaRoot == null)
        {
            Debug.LogWarning(
                "Cannot rotate Chica: missing revealPivot, playerHead, or chicaRoot.",
                this
            );
            return;
        }

        // Direction from Chica herself toward the player.
        Vector3 toPlayer =
            playerHead.position - chicaRoot.transform.position;

        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.001f)
            return;

        toPlayer.Normalize();

        // Use Chica's actual transform orientation instead of assuming
        // that ChicaPivot's +Z axis is her visual forward direction.
        Vector3 chicaForward =
            Vector3.ProjectOnPlane(
                chicaRoot.transform.forward,
                Vector3.up
            );

        if (chicaForward.sqrMagnitude < 0.001f)
            return;

        chicaForward.Normalize();

        // Determine how far Chica must rotate from where she currently
        // faces to point toward the player.
        Quaternion rotationDifference =
            Quaternion.FromToRotation(
                chicaForward,
                toPlayer
            );

        // Rotate the entire reveal rig by that amount.
        revealPivot.rotation =
            rotationDifference * revealPivot.rotation;

        // Optional final visual correction.
        if (Mathf.Abs(chicaYawOffset) > 0.01f)
        {
            revealPivot.Rotate(
                0f,
                chicaYawOffset,
                0f,
                Space.World
            );
        }

        Debug.Log(
            $"CHICA ROTATED | " +
            $"Player: {playerHead.position} | " +
            $"Chica: {chicaRoot.transform.position} | " +
            $"Target Direction: {toPlayer} | " +
            $"Pivot Y: {revealPivot.eulerAngles.y:F1}",
            this
        );
    }
}

using UnityEngine;

public class VRAvatarLocomotionAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator avatarAnimator;

    [Tooltip("Use the same PlayerController transform used by PlayerFootsteps.")]
    [SerializeField] private Transform movementRoot;

    [Tooltip("Use CenterEyeAnchor.")]
    [SerializeField] private Transform playerHead;

    [Header("Movement Detection")]
    [Min(0f)]
    [SerializeField] private float minimumMoveSpeed = 0.08f;

    [Tooltip("At this speed or faster, MoveAmount reaches +/-1.")]
    [Min(0.1f)]
    [SerializeField] private float fullWalkSpeed = 1.25f;

    [Tooltip("Movement larger than this in one frame is treated as teleportation.")]
    [Min(0.1f)]
    [SerializeField] private float teleportDistance = 1.25f;

    [Tooltip("If movement is this strongly opposite the headset's facing direction, use Walk Backwards. Sideways movement uses the normal Walk clip because there is no strafe animation.")]
    [Range(-1f, 0f)]
    [SerializeField] private float backwardsDirectionThreshold = -0.35f;

    [Header("Crouch Detection")]
    [Tooltip("Absolute headset height above the PlayerController origin that counts as seated/crouched. This lets a player who STARTS seated immediately use the crouch animation.")]
    [Min(0.5f)]
    [SerializeField] private float seatedCrouchHeight = 1.35f;

    [Tooltip("Once a standing height has been learned, this additional drop also triggers crouching.")]
    [Min(0.05f)]
    [SerializeField] private float crouchEnterDrop = 0.28f;

    [Tooltip("Crouch ends once the headset is this close to the learned standing height. Keep lower than Crouch Enter Drop.")]
    [Min(0.01f)]
    [SerializeField] private float crouchExitDrop = 0.16f;

    [Tooltip("Extra height above the seated threshold required before a seated player is considered standing. Prevents jitter near the threshold.")]
    [Min(0f)]
    [SerializeField] private float seatedExitMargin = 0.10f;

    [Tooltip("When standing, slowly allow a slightly taller posture to become the new standing baseline.")]
    [Min(0f)]
    [SerializeField] private float standingHeightAdaptSpeed = 0.25f;

    [Header("Animator Parameters")]
    [Tooltip("Float: -1 = backwards, 0 = idle, +1 = forward walk.")]
    [SerializeField] private string moveAmountParameter = "MoveAmount";

    [Tooltip("Float: 0 = standing, 1 = crouched.")]
    [SerializeField] private string crouchAmountParameter = "CrouchAmount";

    [Min(0f)]
    [SerializeField] private float movementDampTime = 0.10f;

    [Min(0f)]
    [SerializeField] private float crouchDampTime = 0.08f;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = false;

    private int moveAmountHash;
    private int crouchAmountHash;
    private bool hasMoveParameter;
    private bool hasCrouchParameter;

    private Vector3 previousMovementPosition;
    private bool movementInitialized;

    private float standingHeadHeight;
    private bool standingHeightKnown;
    private bool isCrouching;

    public float MoveAmount { get; private set; }
    public bool IsCrouching => isCrouching;
    public float CurrentHeadHeight { get; private set; }

    private void Awake()
    {
        if (avatarAnimator == null)
            avatarAnimator = GetComponentInChildren<Animator>(true);

        moveAmountHash = Animator.StringToHash(moveAmountParameter);
        crouchAmountHash = Animator.StringToHash(crouchAmountParameter);

        hasMoveParameter = HasAnimatorParameter(
            avatarAnimator,
            moveAmountHash,
            AnimatorControllerParameterType.Float
        );

        hasCrouchParameter = HasAnimatorParameter(
            avatarAnimator,
            crouchAmountHash,
            AnimatorControllerParameterType.Float
        );

        if (avatarAnimator != null)
            avatarAnimator.applyRootMotion = false;
    }

    private void Start()
    {
        ResetTracking();

        if (avatarAnimator != null && (!hasMoveParameter || !hasCrouchParameter))
        {
            Debug.LogWarning(
                "VRAvatarLocomotionAnimator: Animator Controller needs Float parameters " +
                $"'{moveAmountParameter}' and '{crouchAmountParameter}'.",
                this
            );
        }
    }

    private void LateUpdate()
    {
        if (avatarAnimator == null || movementRoot == null || playerHead == null)
            return;

        UpdateMovement();
        UpdateCrouch();

        if (hasMoveParameter)
        {
            avatarAnimator.SetFloat(
                moveAmountHash,
                MoveAmount,
                movementDampTime,
                Time.deltaTime
            );
        }

        if (hasCrouchParameter)
        {
            avatarAnimator.SetFloat(
                crouchAmountHash,
                isCrouching ? 1f : 0f,
                crouchDampTime,
                Time.deltaTime
            );
        }
    }

    private void UpdateMovement()
    {
        Vector3 currentPosition = movementRoot.position;

        if (!movementInitialized)
        {
            previousMovementPosition = currentPosition;
            movementInitialized = true;
            MoveAmount = 0f;
            return;
        }

        Vector3 delta = currentPosition - previousMovementPosition;
        previousMovementPosition = currentPosition;

        Vector3 horizontalDelta = Vector3.ProjectOnPlane(delta, Vector3.up);
        float frameDistance = horizontalDelta.magnitude;

        if (frameDistance > teleportDistance || Time.deltaTime <= 0f)
        {
            MoveAmount = 0f;
            return;
        }

        float speed = frameDistance / Time.deltaTime;

        if (speed <= minimumMoveSpeed)
        {
            MoveAmount = 0f;
            return;
        }

        float normalizedSpeed = Mathf.InverseLerp(
            minimumMoveSpeed,
            Mathf.Max(minimumMoveSpeed + 0.01f, fullWalkSpeed),
            speed
        );

        Vector3 movementDirection = horizontalDelta.normalized;
        Vector3 headForward = Vector3.ProjectOnPlane(playerHead.forward, Vector3.up);

        if (headForward.sqrMagnitude < 0.001f)
        {
            MoveAmount = normalizedSpeed;
            return;
        }

        headForward.Normalize();
        float forwardDot = Vector3.Dot(movementDirection, headForward);

        // We only have forward/backward clips. Treat strafing as forward walking.
        MoveAmount = forwardDot <= backwardsDirectionThreshold
            ? -normalizedSpeed
            : normalizedSpeed;
    }

    private void UpdateCrouch()
    {
        CurrentHeadHeight = playerHead.position.y - movementRoot.position.y;

        // Important: this absolute check means somebody who starts the experience
        // already sitting down is immediately represented as crouching.
        bool definitelySeated = CurrentHeadHeight <= seatedCrouchHeight;

        if (!standingHeightKnown)
        {
            if (definitelySeated)
            {
                SetCrouching(true);
                return;
            }

            standingHeadHeight = CurrentHeadHeight;
            standingHeightKnown = true;
            SetCrouching(false);
            return;
        }

        if (isCrouching)
        {
            bool highEnoughToLeaveSeatedPose =
                CurrentHeadHeight >= seatedCrouchHeight + seatedExitMargin;

            float dropFromStanding = standingHeadHeight - CurrentHeadHeight;
            bool closeEnoughToStanding = dropFromStanding <= crouchExitDrop;

            if (highEnoughToLeaveSeatedPose && closeEnoughToStanding)
                SetCrouching(false);
        }
        else
        {
            float dropFromStanding = standingHeadHeight - CurrentHeadHeight;

            if (definitelySeated || dropFromStanding >= crouchEnterDrop)
            {
                SetCrouching(true);
            }
            else if (CurrentHeadHeight > standingHeadHeight)
            {
                standingHeadHeight = Mathf.MoveTowards(
                    standingHeadHeight,
                    CurrentHeadHeight,
                    standingHeightAdaptSpeed * Time.deltaTime
                );
            }
        }
    }

    private void SetCrouching(bool value)
    {
        if (isCrouching == value)
            return;

        isCrouching = value;

        if (debugLogging)
            Debug.Log(value ? "Avatar entered crouch." : "Avatar exited crouch.", this);
    }

    public void ResetTracking()
    {
        movementInitialized = false;
        standingHeightKnown = false;
        isCrouching = false;
        MoveAmount = 0f;
        CurrentHeadHeight = 0f;

        if (movementRoot != null)
            previousMovementPosition = movementRoot.position;
    }

    private static bool HasAnimatorParameter(
        Animator targetAnimator,
        int parameterHash,
        AnimatorControllerParameterType parameterType)
    {
        if (targetAnimator == null)
            return false;

        AnimatorControllerParameter[] parameters = targetAnimator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].nameHash == parameterHash &&
                parameters[i].type == parameterType)
            {
                return true;
            }
        }

        return false;
    }
}

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

    [Tooltip("At this speed or faster, MoveAmount reaches 1.")]
    [Min(0.1f)]
    [SerializeField] private float fullWalkSpeed = 1.25f;

    [Tooltip("Movement larger than this in one frame is treated as teleportation.")]
    [Min(0.1f)]
    [SerializeField] private float teleportDistance = 1.25f;

    [Header("Crouch Detection")]
    [Tooltip("How far the headset must drop from the calibrated standing height before crouching begins.")]
    [Min(0.05f)]
    [SerializeField] private float crouchEnterDrop = 0.28f;

    [Tooltip("Crouch ends once the headset is this close to standing height. Keep lower than Crouch Enter Drop.")]
    [Min(0.01f)]
    [SerializeField] private float crouchExitDrop = 0.16f;

    [Tooltip("When standing, slowly allow a slightly taller measured posture to become the new baseline.")]
    [Min(0f)]
    [SerializeField] private float standingHeightAdaptSpeed = 0.25f;

    [Header("Animator Parameters")]
    [SerializeField] private string moveAmountParameter = "MoveAmount";
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
    private bool heightInitialized;
    private bool isCrouching;

    public float MoveAmount { get; private set; }
    public bool IsCrouching => isCrouching;

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

        Vector2 horizontalDelta = new Vector2(delta.x, delta.z);
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

        MoveAmount = Mathf.InverseLerp(
            minimumMoveSpeed,
            Mathf.Max(minimumMoveSpeed + 0.01f, fullWalkSpeed),
            speed
        );
    }

    private void UpdateCrouch()
    {
        float currentHeadHeight = playerHead.position.y - movementRoot.position.y;

        if (!heightInitialized)
        {
            standingHeadHeight = currentHeadHeight;
            heightInitialized = true;
            return;
        }

        float dropFromStanding = standingHeadHeight - currentHeadHeight;

        if (!isCrouching)
        {
            if (dropFromStanding >= crouchEnterDrop)
            {
                isCrouching = true;

                if (debugLogging)
                    Debug.Log("Avatar entered crouch.", this);
            }
            else if (currentHeadHeight > standingHeadHeight)
            {
                // This adapts to users settling into a slightly taller natural
                // posture without using a fixed real-world player height.
                standingHeadHeight = Mathf.MoveTowards(
                    standingHeadHeight,
                    currentHeadHeight,
                    standingHeightAdaptSpeed * Time.deltaTime
                );
            }
        }
        else if (dropFromStanding <= crouchExitDrop)
        {
            isCrouching = false;

            if (debugLogging)
                Debug.Log("Avatar exited crouch.", this);
        }
    }

    public void ResetTracking()
    {
        movementInitialized = false;
        heightInitialized = false;
        isCrouching = false;
        MoveAmount = 0f;

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

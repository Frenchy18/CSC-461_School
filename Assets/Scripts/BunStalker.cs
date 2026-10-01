using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BunStalker : MonoBehaviour
{
    public enum ActivationCondition
    {
        MuseumComplete,
        HeadphonesWorn,
        Either,
        Both
    }

    [Header("Activation")]
    [SerializeField] private ActivationCondition activationCondition = ActivationCondition.Either;
    [SerializeField] private ExhibitVisitTracker tracker;
    [SerializeField] private WearableHeadphones headphones;
    [SerializeField] private bool stayActiveOnceTriggered = true;
    [Min(0f)]
    [SerializeField] private float activationDelay = 0.75f;

    [Header("Player")]
    [Tooltip("Use CenterEyeAnchor's Camera component.")]
    [SerializeField] private Camera playerCamera;

    [Tooltip("Use the transform Bun should path toward. PlayerController is a good choice.")]
    [SerializeField] private Transform chaseTarget;

    [Tooltip("Use the top-level Meta/XR player rig so sight raycasts ignore the player's own colliders.")]
    [SerializeField] private Transform playerRigRoot;

    [Header("Movement")]
    [SerializeField] private NavMeshAgent agent;
    [Min(0.25f)]
    [SerializeField] private float stoppingDistance = 1.35f;
    [Min(0.02f)]
    [SerializeField] private float destinationRefreshInterval = 0.10f;
    [Min(0.1f)]
    [SerializeField] private float targetSampleRadius = 2.0f;

    [Header("Line Of Sight")]
    [Tooltip("Optional head/chest/pelvis markers. If empty, Bun's renderer bounds are sampled automatically.")]
    [SerializeField] private Transform[] visibilityPoints;

    [Tooltip("Layers that can block the player's view of Bun.")]
    [SerializeField] private LayerMask sightMask = ~0;

    [Range(0f, 0.2f)]
    [SerializeField] private float viewportPadding = 0.02f;

    [Min(0f)]
    [SerializeField] private float lineOfSightSkin = 0.05f;

    [Tooltip("Prevents rapid start/stop jitter when Bun is barely leaving the edge of view.")]
    [Min(0f)]
    [SerializeField] private float unseenResumeDelay = 0.10f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkingBoolName = "Walking";

    [Header("Debug")]
    [SerializeField] private bool debugLogging = false;

    private readonly RaycastHit[] sightHits = new RaycastHit[24];

    private Renderer[] bunRenderers;
    private int walkingBoolHash;
    private bool hasWalkingParameter;
    private bool hasActivated;
    private float activationTimer;
    private float lastVisibleTime = float.NegativeInfinity;
    private float nextDestinationRefreshTime;

    public bool HasActivated => hasActivated;
    public bool IsVisibleToPlayer { get; private set; }
    public bool IsMoving { get; private set; }

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (tracker == null)
            tracker = ExhibitVisitTracker.Instance;

        if (headphones == null)
            headphones = FindFirstObjectByType<WearableHeadphones>();

        bunRenderers = GetComponentsInChildren<Renderer>(true);

        walkingBoolHash = Animator.StringToHash(walkingBoolName);
        hasWalkingParameter = HasAnimatorParameter(
            animator,
            walkingBoolHash,
            AnimatorControllerParameterType.Bool
        );

        if (agent != null)
        {
            agent.stoppingDistance = stoppingDistance;
            agent.updatePosition = true;
            agent.updateRotation = true;
        }
    }

    private void Start()
    {
        StopBun();

        if (animator != null && !hasWalkingParameter)
        {
            Debug.LogWarning(
                $"BunStalker: Animator does not contain a bool parameter named '{walkingBoolName}'. " +
                "Bun can still move, but the walk animation will not switch until that parameter is added.",
                this
            );
        }
    }

    private void Update()
    {
        // Scene execution order can mean the tracker did not yet exist during Awake.
        if (tracker == null)
            tracker = ExhibitVisitTracker.Instance;

        bool activationConditionMet = IsActivationConditionMet();

        if (!hasActivated)
        {
            if (activationConditionMet)
            {
                activationTimer += Time.deltaTime;

                if (activationTimer >= activationDelay)
                {
                    hasActivated = true;

                    if (debugLogging)
                        Debug.Log("Bun has awakened.", this);
                }
            }
            else
            {
                activationTimer = 0f;
            }
        }

        bool stalkingEnabled = hasActivated &&
            (stayActiveOnceTriggered || activationConditionMet);

        if (!stalkingEnabled)
        {
            IsVisibleToPlayer = false;
            StopBun();
            return;
        }

        IsVisibleToPlayer = CheckVisibleToPlayer();

        if (IsVisibleToPlayer)
            lastVisibleTime = Time.time;

        bool freezeBecauseObserved =
            IsVisibleToPlayer ||
            Time.time - lastVisibleTime < unseenResumeDelay;

        if (freezeBecauseObserved)
        {
            StopBun();
            return;
        }

        ChasePlayer();
    }

    private bool IsActivationConditionMet()
    {
        bool museumComplete = tracker != null && tracker.IsComplete;
        bool headphonesWorn = headphones != null && headphones.IsWorn;

        switch (activationCondition)
        {
            case ActivationCondition.MuseumComplete:
                return museumComplete;

            case ActivationCondition.HeadphonesWorn:
                return headphonesWorn;

            case ActivationCondition.Both:
                return museumComplete && headphonesWorn;

            default:
                return museumComplete || headphonesWorn;
        }
    }

    private void ChasePlayer()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh || chaseTarget == null)
        {
            SetWalking(false);
            return;
        }

        Vector3 flatDelta = chaseTarget.position - transform.position;
        flatDelta.y = 0f;

        if (flatDelta.magnitude <= stoppingDistance)
        {
            StopBun();
            return;
        }

        agent.isStopped = false;

        if (Time.time >= nextDestinationRefreshTime)
        {
            nextDestinationRefreshTime = Time.time + destinationRefreshInterval;

            Vector3 desiredDestination = chaseTarget.position;

            if (NavMesh.SamplePosition(
                    desiredDestination,
                    out NavMeshHit navHit,
                    targetSampleRadius,
                    agent.areaMask))
            {
                desiredDestination = navHit.position;
            }

            agent.SetDestination(desiredDestination);
        }

        bool moving = agent.velocity.sqrMagnitude > 0.0025f ||
                      (agent.hasPath && agent.remainingDistance > agent.stoppingDistance);

        SetWalking(moving);
    }

    private void StopBun()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = true;

        SetWalking(false);
    }

    private void SetWalking(bool walking)
    {
        IsMoving = walking;

        if (animator != null && hasWalkingParameter)
            animator.SetBool(walkingBoolHash, walking);
    }

    private bool CheckVisibleToPlayer()
    {
        if (playerCamera == null)
            return false;

        if (visibilityPoints != null && visibilityPoints.Length > 0)
        {
            for (int i = 0; i < visibilityPoints.Length; i++)
            {
                Transform point = visibilityPoints[i];

                if (point != null && IsPointVisible(point.position))
                    return true;
            }

            return false;
        }

        if (!TryGetCombinedRendererBounds(out Bounds bounds))
            return IsPointVisible(transform.position + Vector3.up);

        Vector3 center = bounds.center;
        float x = bounds.extents.x * 0.65f;
        float y = bounds.extents.y * 0.70f;

        // Sampling several points prevents Bun from moving just because his
        // pivot or chest is hidden while another visible part is on-screen.
        return IsPointVisible(center) ||
               IsPointVisible(center + Vector3.up * y) ||
               IsPointVisible(center - Vector3.up * y) ||
               IsPointVisible(center + playerCamera.transform.right * x) ||
               IsPointVisible(center - playerCamera.transform.right * x);
    }

    private bool IsPointVisible(Vector3 worldPoint)
    {
        Vector3 viewport = playerCamera.WorldToViewportPoint(worldPoint);

        if (viewport.z <= 0f)
            return false;

        if (viewport.x < -viewportPadding ||
            viewport.x > 1f + viewportPadding ||
            viewport.y < -viewportPadding ||
            viewport.y > 1f + viewportPadding)
        {
            return false;
        }

        return !IsSightBlocked(worldPoint);
    }

    private bool IsSightBlocked(Vector3 worldPoint)
    {
        Vector3 origin = playerCamera.transform.position;
        Vector3 toPoint = worldPoint - origin;
        float distance = toPoint.magnitude;

        if (distance <= 0.001f)
            return false;

        float castDistance = Mathf.Max(0f, distance - lineOfSightSkin);
        Vector3 direction = toPoint / distance;

        int hitCount = Physics.RaycastNonAlloc(
            origin,
            direction,
            sightHits,
            castDistance,
            sightMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = sightHits[i].collider;

            if (hitCollider == null)
                continue;

            Transform hitTransform = hitCollider.transform;

            // Bun's own colliders should never count as an obstruction.
            if (hitTransform == transform ||
                hitTransform.IsChildOf(transform) ||
                transform.IsChildOf(hitTransform))
            {
                continue;
            }

            // Ignore headset/controllers/body colliders belonging to the player.
            if (playerRigRoot != null &&
                (hitTransform == playerRigRoot || hitTransform.IsChildOf(playerRigRoot)))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool TryGetCombinedRendererBounds(out Bounds bounds)
    {
        bounds = default;
        bool foundAny = false;

        if (bunRenderers == null)
            return false;

        for (int i = 0; i < bunRenderers.Length; i++)
        {
            Renderer renderer = bunRenderers[i];

            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            if (!foundAny)
            {
                bounds = renderer.bounds;
                foundAny = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return foundAny;
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

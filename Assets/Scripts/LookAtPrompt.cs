using UnityEngine;

public class LookAtPrompt : MonoBehaviour
{
    [Header("Viewer")]
    [SerializeField] private Transform viewer;

    [Header("Target")]
    [SerializeField] private Collider targetCollider;

    [Header("Prompt")]
    [SerializeField] private CanvasGroup promptCanvas;

    [Header("Optional Wearable")]
    [SerializeField] private WearableHeadphones wearable;

    [Header("Look Detection")]
    [SerializeField] private float maxDistance = 2.5f;

    [Range(1f, 45f)]
    [SerializeField] private float maxLookAngle = 15f;

    [Header("Fade")]
    [SerializeField] private float fadeSpeed = 5f;

    private void Awake()
    {
        if (promptCanvas != null)
        {
            promptCanvas.alpha = 0f;
            promptCanvas.interactable = false;
            promptCanvas.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        if (promptCanvas == null || viewer == null)
            return;

        bool shouldShow = ShouldShowPrompt();

        float targetAlpha = shouldShow ? 1f : 0f;

        promptCanvas.alpha = Mathf.MoveTowards(
            promptCanvas.alpha,
            targetAlpha,
            fadeSpeed * Time.deltaTime
        );
    }

    private bool ShouldShowPrompt()
    {
        // Don't show "Put these on" while they're already being worn.
        if (wearable != null && wearable.IsWorn)
            return false;

        Vector3 targetPosition;

        if (targetCollider != null)
        {
            targetPosition = targetCollider.bounds.center;
        }
        else
        {
            targetPosition = transform.position;
        }

        Vector3 toTarget = targetPosition - viewer.position;

        float distance = toTarget.magnitude;

        if (distance > maxDistance)
            return false;

        Vector3 direction = toTarget.normalized;

        float angle = Vector3.Angle(
            viewer.forward,
            direction
        );

        return angle <= maxLookAngle;
    }
}

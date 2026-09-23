using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class ExhibitTrigger : MonoBehaviour
{
    [Header("Exhibit")]
    [Tooltip("Must be unique, for example Lighting, Composition, Editing, Sound, Color.")]
    [SerializeField] private string exhibitId = "Exhibit01";

    [Tooltip("Optional. If left empty, the scene's ExhibitVisitTracker is used.")]
    [SerializeField] private ExhibitVisitTracker tracker;

    [Header("Player Detection")]
    [Tooltip(
        "Drag the Meta PlayerController here. Only that transform (and its children) " +
        "will activate this exhibit.")]
    [SerializeField] private Transform playerRoot;

    [Header("UI To Fade")]
    [Tooltip(
        "Add CanvasGroup components to the World Space canvases you want to appear " +
        "when the player enters this trigger.")]
    [SerializeField] private CanvasGroup[] uiGroups;

    [Min(0f)]
    [SerializeField] private float fadeInDuration = 0.45f;

    [Min(0f)]
    [SerializeField] private float fadeOutDuration = 0.30f;

    [Tooltip(
        "If enabled, the UI stays visible after the exhibit is first entered. " +
        "Leave this off for proximity-based museum panels.")]
    [SerializeField] private bool stayVisibleAfterVisit = false;

    private BoxCollider _triggerCollider;
    private Rigidbody _rigidbody;

    private int _playerColliderCount;
    private float _targetAlpha;

    private void Awake()
    {
        _triggerCollider = GetComponent<BoxCollider>();
        _triggerCollider.isTrigger = true;

        // A kinematic Rigidbody on the trigger volume makes trigger callbacks
        // reliable without allowing the trigger box to move under physics.
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;

        if (tracker == null)
            tracker = ExhibitVisitTracker.Instance;

        SetUIImmediate(0f);
    }

    private void Start()
    {
        // The tracker may initialize after this object depending on scene order.
        if (tracker == null)
            tracker = ExhibitVisitTracker.Instance;

        if (tracker == null)
            tracker = FindFirstObjectByType<ExhibitVisitTracker>();
    }

    private void Update()
    {
        float currentAlpha = GetCurrentAlpha();

        if (Mathf.Approximately(currentAlpha, _targetAlpha))
            return;

        float duration = _targetAlpha > currentAlpha
            ? fadeInDuration
            : fadeOutDuration;

        float nextAlpha;

        if (duration <= 0f)
        {
            nextAlpha = _targetAlpha;
        }
        else
        {
            nextAlpha = Mathf.MoveTowards(
                currentAlpha,
                _targetAlpha,
                Time.deltaTime / duration);
        }

        ApplyAlpha(nextAlpha);

        // Make the UI clickable only once it is essentially fully visible.
        bool interactive = nextAlpha >= 0.98f && _targetAlpha > 0.5f;
        SetInteraction(interactive);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        _playerColliderCount++;

        if (_playerColliderCount != 1)
            return;

        _targetAlpha = 1f;

        if (tracker != null)
            tracker.VisitExhibit(exhibitId);
        else
            Debug.LogWarning(
                $"Exhibit '{exhibitId}' has no ExhibitVisitTracker assigned.",
                this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        _playerColliderCount = Mathf.Max(0, _playerColliderCount - 1);

        if (_playerColliderCount > 0)
            return;

        bool alreadyVisited = tracker != null && tracker.HasVisited(exhibitId);

        if (stayVisibleAfterVisit && alreadyVisited)
            return;

        _targetAlpha = 0f;

        // Immediately stop invisible/fading UI from receiving VR ray clicks.
        SetInteraction(false);
    }

    private bool IsPlayer(Collider other)
    {
        if (playerRoot == null || other == null)
            return false;

        Transform otherTransform = other.transform;

        return otherTransform == playerRoot
            || otherTransform.IsChildOf(playerRoot)
            || playerRoot.IsChildOf(otherTransform);
    }

    private float GetCurrentAlpha()
    {
        if (uiGroups == null || uiGroups.Length == 0)
            return _targetAlpha;

        foreach (CanvasGroup group in uiGroups)
        {
            if (group != null)
                return group.alpha;
        }

        return _targetAlpha;
    }

    private void SetUIImmediate(float alpha)
    {
        _targetAlpha = alpha;
        ApplyAlpha(alpha);
        SetInteraction(alpha >= 0.98f);
    }

    private void ApplyAlpha(float alpha)
    {
        if (uiGroups == null)
            return;

        for (int i = 0; i < uiGroups.Length; i++)
        {
            if (uiGroups[i] != null)
                uiGroups[i].alpha = alpha;
        }
    }

    private void SetInteraction(bool enabled)
    {
        if (uiGroups == null)
            return;

        for (int i = 0; i < uiGroups.Length; i++)
        {
            CanvasGroup group = uiGroups[i];

            if (group == null)
                continue;

            group.interactable = enabled;
            group.blocksRaycasts = enabled;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
            box.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }
#endif
}
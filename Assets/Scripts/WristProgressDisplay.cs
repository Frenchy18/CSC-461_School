using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class WristProgressDisplay : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The museum-wide ExhibitVisitTracker. If empty, the script finds it automatically.")]
    [SerializeField] private ExhibitVisitTracker tracker;

    [Tooltip("Usually CenterEyeAnchor. If empty, Camera.main is used.")]
    [SerializeField] private Transform playerHead;

    [Tooltip("The transform whose face direction should point toward the player. ProgressCanvas itself is a good default.")]
    [SerializeField] private Transform watchFace;

    [SerializeField] private TMP_Text progressTitle;
    [SerializeField] private TMP_Text progressCount;

    [Tooltip("Optional TMP text. Create a child named ProgressDots if you want ● ○ style progress.")]
    [SerializeField] private TMP_Text progressDots;

    [Header("Display Text")]
    [SerializeField] private string titleText = "MUSEUM PROGRESS";
    [SerializeField] private string countFormat = "{0} / {1}";
    [SerializeField] private string visitedDot = "●";
    [SerializeField] private string unvisitedDot = "○";
    [SerializeField] private string dotSeparator = "  ";

    [Header("Raise / Look Detection")]
    [Tooltip("The wrist must be within this distance of the player's head to count as raised.")]
    [Min(0.1f)]
    [SerializeField] private float maxHeadDistance = 0.80f;

    [Tooltip("How directly the player must look toward the watch. Larger values are more forgiving.")]
    [Range(5f, 90f)]
    [SerializeField] private float maxLookAngle = 38f;

    [Tooltip("How directly the watch face must point toward the player's head. Larger values are more forgiving.")]
    [Range(5f, 90f)]
    [SerializeField] private float maxFaceAngle = 65f;

    [Tooltip("Toggle this if the Canvas forward direction points away from the visible watch face.")]
    [SerializeField] private bool invertWatchFaceDirection = false;

    [Tooltip("Keeps the UI visible briefly when the wrist moves around the activation threshold, preventing flicker.")]
    [Min(0f)]
    [SerializeField] private float visibleHoldTime = 0.15f;

    [Header("Fade")]
    [Min(0f)]
    [SerializeField] private float fadeInDuration = 0.25f;

    [Min(0f)]
    [SerializeField] private float fadeOutDuration = 0.20f;

    [Header("Debug")]
    [SerializeField] private bool showDebugValues = false;

    private CanvasGroup canvasGroup;
    private float hideAtTime;
    private int lastVisited = -1;
    private int lastTotal = -1;

    private void Reset()
    {
        watchFace = transform;
        AutoFindChildText();
    }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (watchFace == null)
            watchFace = transform;

        if (tracker == null)
            tracker = FindFirstObjectByType<ExhibitVisitTracker>();

        if (playerHead == null)
        {
            if (Camera.main != null)
                playerHead = Camera.main.transform;
            else
            {
                GameObject centerEye = GameObject.Find("CenterEyeAnchor");
                if (centerEye != null)
                    playerHead = centerEye.transform;
            }
        }

        AutoFindChildText();
        RefreshProgress(force: true);
    }

    private void Update()
    {
        if (tracker == null)
            tracker = ExhibitVisitTracker.Instance != null
                ? ExhibitVisitTracker.Instance
                : FindFirstObjectByType<ExhibitVisitTracker>();

        RefreshProgress(force: false);

        bool shouldShow = ShouldShowWatchUI();

        if (shouldShow)
            hideAtTime = Time.time + visibleHoldTime;

        bool visibleRequested = shouldShow || Time.time < hideAtTime;
        FadeTo(visibleRequested ? 1f : 0f);

        if (showDebugValues && playerHead != null && watchFace != null)
            DrawDebug();
    }

    private bool ShouldShowWatchUI()
    {
        if (playerHead == null || watchFace == null)
            return false;

        Vector3 headToWatch = watchFace.position - playerHead.position;
        float distance = headToWatch.magnitude;

        if (distance <= 0.001f || distance > maxHeadDistance)
            return false;

        Vector3 directionToWatch = headToWatch / distance;
        float lookAngle = Vector3.Angle(playerHead.forward, directionToWatch);

        if (lookAngle > maxLookAngle)
            return false;

        Vector3 faceDirection = invertWatchFaceDirection
            ? -watchFace.forward
            : watchFace.forward;

        Vector3 directionToHead = -directionToWatch;
        float faceAngle = Vector3.Angle(faceDirection, directionToHead);

        return faceAngle <= maxFaceAngle;
    }

    private void FadeTo(float target)
    {
        float current = canvasGroup.alpha;

        if (Mathf.Approximately(current, target))
            return;

        float duration = target > current ? fadeInDuration : fadeOutDuration;

        if (duration <= 0f)
            canvasGroup.alpha = target;
        else
            canvasGroup.alpha = Mathf.MoveTowards(current, target, Time.deltaTime / duration);

        // The watch is informational only, so never let it steal VR/UI ray input.
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void RefreshProgress(bool force)
    {
        if (tracker == null)
            return;

        int visited = tracker.VisitedCount;
        int total = tracker.TotalExhibits;

        if (!force && visited == lastVisited && total == lastTotal)
            return;

        lastVisited = visited;
        lastTotal = total;

        if (progressTitle != null)
            progressTitle.text = titleText;

        if (progressCount != null)
            progressCount.text = string.Format(countFormat, visited, total);

        if (progressDots != null)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            for (int i = 0; i < total; i++)
            {
                if (i > 0)
                    builder.Append(dotSeparator);

                builder.Append(i < visited ? visitedDot : unvisitedDot);
            }

            progressDots.text = builder.ToString();
        }
    }

    private void AutoFindChildText()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text text in texts)
        {
            if (text == null)
                continue;

            if (progressTitle == null && text.gameObject.name == "ProgressTitle")
                progressTitle = text;
            else if (progressCount == null && text.gameObject.name == "ProgressCount")
                progressCount = text;
            else if (progressDots == null && text.gameObject.name == "ProgressDots")
                progressDots = text;
        }
    }

    private void DrawDebug()
    {
        Vector3 watchPosition = watchFace.position;
        Debug.DrawLine(playerHead.position, watchPosition, Color.cyan);

        Vector3 faceDirection = invertWatchFaceDirection
            ? -watchFace.forward
            : watchFace.forward;

        Debug.DrawRay(watchPosition, faceDirection * 0.15f, Color.green);
    }
}
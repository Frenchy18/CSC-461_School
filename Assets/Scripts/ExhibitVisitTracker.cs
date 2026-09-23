using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ExhibitVisitTracker : MonoBehaviour
{
    public static ExhibitVisitTracker Instance { get; private set; }

    [Header("Museum Progress")]
    [Min(1)]
    [SerializeField] private int totalExhibits = 5;

    [Header("Optional UI")]
    [Tooltip("Optional TextMeshPro label, for example: Exhibits Visited: 2 / 5")]
    [SerializeField] private TMP_Text progressText;

    [SerializeField] private string progressFormat = "Exhibits Visited: {0} / {1}";

    private readonly HashSet<string> _visitedExhibits = new HashSet<string>();

    public int VisitedCount => _visitedExhibits.Count;
    public int TotalExhibits => totalExhibits;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "More than one ExhibitVisitTracker exists in the scene. " +
                "Only one tracker should be active.",
                this);
            return;
        }

        Instance = this;
        RefreshProgressUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool VisitExhibit(string exhibitId)
    {
        if (string.IsNullOrWhiteSpace(exhibitId))
        {
            Debug.LogWarning(
                "An ExhibitTrigger tried to register a visit without an Exhibit ID.",
                this);
            return false;
        }

        // HashSet.Add returns false when this exhibit was already visited.
        if (!_visitedExhibits.Add(exhibitId))
            return false;

        RefreshProgressUI();

        Debug.Log(
            $"Visited exhibit '{exhibitId}'. Progress: {VisitedCount}/{totalExhibits}",
            this);

        return true;
    }

    public bool HasVisited(string exhibitId)
    {
        return !string.IsNullOrWhiteSpace(exhibitId)
            && _visitedExhibits.Contains(exhibitId);
    }

    public void ResetVisits()
    {
        _visitedExhibits.Clear();
        RefreshProgressUI();
    }

    private void RefreshProgressUI()
    {
        if (progressText == null)
            return;

        progressText.text = string.Format(
            progressFormat,
            VisitedCount,
            totalExhibits);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (totalExhibits < 1)
            totalExhibits = 1;

        if (Application.isPlaying)
            RefreshProgressUI();
    }
#endif
}

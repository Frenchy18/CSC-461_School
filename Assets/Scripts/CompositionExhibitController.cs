using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CameraCompositionExhibitController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Camera exhibitCamera;
    [SerializeField] private Transform subjectRoot;
    [SerializeField] private RawImage monitorImage;
    [SerializeField] private TMP_Text shotTitleText;
    [SerializeField] private TMP_Text shotDescriptionText;

    [Header("Camera Positions")]
    [SerializeField] private Transform widePosition;
    [SerializeField] private Transform mediumPosition;
    [SerializeField] private Transform closeUpPosition;
    [SerializeField] private Transform lowAnglePosition;
    [SerializeField] private Transform highAnglePosition;

    [Header("Buttons")]
    [SerializeField] private Button wideButton;
    [SerializeField] private Button mediumButton;
    [SerializeField] private Button closeUpButton;
    [SerializeField] private Button lowAngleButton;
    [SerializeField] private Button highAngleButton;

    [Header("Monitor Render Texture")]
    [SerializeField, Min(320)] private int renderWidth = 960;
    [SerializeField, Min(180)] private int renderHeight = 540;

    [Header("Aiming")]
    [Tooltip("When enabled, the exhibit camera automatically points toward Fred instead of using the marker's rotation.")]
    [SerializeField] private bool autoAimAtSubject = true;

    private RenderTexture runtimeRenderTexture;

    private void Awake()
    {
        AutoFindReferences();
        CreateMonitorTexture();
        HookButtons();
    }

    private void Start()
    {
        SetWide();
    }

    private void OnDestroy()
    {
        UnhookButtons();

        if (exhibitCamera != null && exhibitCamera.targetTexture == runtimeRenderTexture)
            exhibitCamera.targetTexture = null;

        if (runtimeRenderTexture != null)
        {
            runtimeRenderTexture.Release();
            Destroy(runtimeRenderTexture);
        }
    }

    public void SetWide()
    {
        ApplyShot(
            widePosition,
            50f,
            1.0f,
            "WIDE SHOT",
            "Shows the subject together with the surrounding environment. Wide shots establish location, spatial relationships, and context."
        );
    }

    public void SetMedium()
    {
        ApplyShot(
            mediumPosition,
            45f,
            1.30f,
            "MEDIUM SHOT",
            "Frames the subject more closely while preserving body language and some environment. It is common for dialogue and everyday action."
        );
    }

    public void SetCloseUp()
    {
        ApplyShot(
            closeUpPosition,
            40f,
            1.55f,
            "CLOSE-UP",
            "Emphasizes the face and small details. Close-ups direct attention toward emotion, reaction, and important visual information."
        );
    }

    public void SetLowAngle()
    {
        ApplyShot(
            lowAnglePosition,
            45f,
            1.35f,
            "LOW ANGLE",
            "Looks upward at the subject. A low camera position can make a subject appear larger, imposing, dominant, or visually powerful."
        );
    }

    public void SetHighAngle()
    {
        ApplyShot(
            highAnglePosition,
            45f,
            1.05f,
            "HIGH ANGLE",
            "Looks downward at the subject. A high camera position can make the subject appear smaller and can emphasize the surrounding space."
        );
    }

    private void ApplyShot(Transform marker, float fieldOfView, float targetHeight, string title, string description)
    {
        if (exhibitCamera == null || marker == null)
            return;

        exhibitCamera.transform.position = marker.position;
        exhibitCamera.fieldOfView = fieldOfView;

        if (autoAimAtSubject && subjectRoot != null)
        {
            Vector3 target = subjectRoot.position + Vector3.up * targetHeight;
            Vector3 direction = target - exhibitCamera.transform.position;

            if (direction.sqrMagnitude > 0.001f)
                exhibitCamera.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
        else
        {
            exhibitCamera.transform.rotation = marker.rotation;
        }

        if (shotTitleText != null)
            shotTitleText.text = title;

        if (shotDescriptionText != null)
            shotDescriptionText.text = description;
    }

    private void CreateMonitorTexture()
    {
        if (exhibitCamera == null || monitorImage == null)
            return;

        if (runtimeRenderTexture != null)
            return;

        runtimeRenderTexture = new RenderTexture(renderWidth, renderHeight, 16, RenderTextureFormat.ARGB32)
        {
            name = "Exhibit2_CameraFeed_Runtime"
        };
        runtimeRenderTexture.Create();

        exhibitCamera.targetTexture = runtimeRenderTexture;
        monitorImage.texture = runtimeRenderTexture;
    }

    private void HookButtons()
    {
        if (wideButton != null) wideButton.onClick.AddListener(SetWide);
        if (mediumButton != null) mediumButton.onClick.AddListener(SetMedium);
        if (closeUpButton != null) closeUpButton.onClick.AddListener(SetCloseUp);
        if (lowAngleButton != null) lowAngleButton.onClick.AddListener(SetLowAngle);
        if (highAngleButton != null) highAngleButton.onClick.AddListener(SetHighAngle);
    }

    private void UnhookButtons()
    {
        if (wideButton != null) wideButton.onClick.RemoveListener(SetWide);
        if (mediumButton != null) mediumButton.onClick.RemoveListener(SetMedium);
        if (closeUpButton != null) closeUpButton.onClick.RemoveListener(SetCloseUp);
        if (lowAngleButton != null) lowAngleButton.onClick.RemoveListener(SetLowAngle);
        if (highAngleButton != null) highAngleButton.onClick.RemoveListener(SetHighAngle);
    }

    private void AutoFindReferences()
    {
        if (exhibitCamera == null)
            exhibitCamera = FindComponentInChildrenByName<Camera>(transform, "ExhibitCamera");

        if (subjectRoot == null)
        {
            Transform found = FindChildRecursive(transform, "Fred_Exhibit2");
            if (found != null) subjectRoot = found;
        }

        if (monitorImage == null)
            monitorImage = FindComponentInChildrenByName<RawImage>(transform, "CameraFeed");

        if (shotTitleText == null)
            shotTitleText = FindComponentInChildrenByName<TMP_Text>(transform, "ShotTitle");

        if (shotDescriptionText == null)
            shotDescriptionText = FindComponentInChildrenByName<TMP_Text>(transform, "ShotDescription");

        widePosition ??= FindChildRecursive(transform, "WidePosition");
        mediumPosition ??= FindChildRecursive(transform, "MediumPosition");
        closeUpPosition ??= FindChildRecursive(transform, "CloseUpPosition");
        lowAnglePosition ??= FindChildRecursive(transform, "LowAnglePosition");
        highAnglePosition ??= FindChildRecursive(transform, "HighAnglePosition");

        wideButton ??= FindComponentInChildrenByName<Button>(transform, "Butt_Wide");
        mediumButton ??= FindComponentInChildrenByName<Button>(transform, "Butt_Medium");
        closeUpButton ??= FindComponentInChildrenByName<Button>(transform, "Butt_CloseUp");
        lowAngleButton ??= FindComponentInChildrenByName<Button>(transform, "Butt_LowAngle");
        highAngleButton ??= FindComponentInChildrenByName<Button>(transform, "Butt_HighAngle");
    }

    private static Transform FindChildRecursive(Transform root, string objectName)
    {
        foreach (Transform child in root)
        {
            if (child.name == objectName)
                return child;

            Transform result = FindChildRecursive(child, objectName);
            if (result != null)
                return result;
        }

        return null;
    }

    private static T FindComponentInChildrenByName<T>(Transform root, string objectName) where T : Component
    {
        Transform target = FindChildRecursive(root, objectName);
        return target != null ? target.GetComponent<T>() : null;
    }
}
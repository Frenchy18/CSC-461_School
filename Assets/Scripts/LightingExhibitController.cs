using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the LightingExhibit currently laid out as:
/// LightingExhibit
///   Lights/
///     Left Spot Light   (Key)
///     Right Spot Light  (Fill)
///     Rear Spot Light   (Rim)
///   Controls/Canvas_Buttons/
///     Butt_3point
///     Butt_HighKey
///     Butt_LowKey
///     Butt_Horror
///   Environment/Fred_Exhibit1
///   LightingExhibitController  <-- attach this script here
///
/// The script auto-finds the existing scene objects by name, so inspector
/// assignment is optional as long as the hierarchy/names stay the same.
/// </summary>
public class LightingExhibitController : MonoBehaviour
{
    [Header("Scene References (auto-filled at runtime if left empty)")]
    [SerializeField] private Light leftSpotLight;   // Key
    [SerializeField] private Light rightSpotLight;  // Fill
    [SerializeField] private Light rearSpotLight;   // Rim

    [SerializeField] private Transform subjectRoot;
    [SerializeField] private Transform buttonCanvas;

    [Header("Buttons (auto-filled at runtime if left empty)")]
    [SerializeField] private Button threePointButton;
    [SerializeField] private Button highKeyButton;
    [SerializeField] private Button lowKeyButton;
    [SerializeField] private Button horrorButton;

    [Header("Preset Description Text (auto-filled at runtime if left empty)")]
    [SerializeField] private TMP_Text threePointText;
    [SerializeField] private TMP_Text highKeyText;
    [SerializeField] private TMP_Text lowKeyText;
    [SerializeField] private TMP_Text horrorText;

    [Header("Horror Underlight Placement")]
    [Tooltip("Approximate height of Fred's face above the character root.")]
    [SerializeField] private float faceHeight = 1.55f;

    [Tooltip("Height of the horror key light above the character root.")]
    [SerializeField] private float horrorKeyHeight = 0.70f;

    [Tooltip("How far in front of the subject to place the horror underlight.")]
    [SerializeField] private float horrorKeyForwardDistance = 0.65f;

    private Vector3 keyDefaultPosition;
    private Quaternion keyDefaultRotation;
    private Vector3 fillDefaultPosition;
    private Quaternion fillDefaultRotation;
    private Vector3 rimDefaultPosition;
    private Quaternion rimDefaultRotation;

    private Transform exhibitRoot;
    private bool referencesValid;

    private void Reset()
    {
        // Unity calls Reset when the component is first added in the Editor.
        // This fills the Inspector automatically when the current hierarchy is intact.
        exhibitRoot = FindExhibitRoot();
        AutoWireReferences();
    }

    private void Awake()
    {
        exhibitRoot = FindExhibitRoot();
        AutoWireReferences();

        referencesValid = ValidateRequiredReferences();
        if (!referencesValid)
            return;

        CacheDefaultLightTransforms();
        WritePresetDescriptions();
        HookButtons();

        // Make Three-Point the default exhibit state.
        SetThreePoint();
    }

    private void OnDestroy()
    {
        if (threePointButton != null) threePointButton.onClick.RemoveListener(SetThreePoint);
        if (highKeyButton != null) highKeyButton.onClick.RemoveListener(SetHighKey);
        if (lowKeyButton != null) lowKeyButton.onClick.RemoveListener(SetLowKey);
        if (horrorButton != null) horrorButton.onClick.RemoveListener(SetHorror);
    }

    private Transform FindExhibitRoot()
    {
        if (transform.name == "LightingExhibit")
            return transform;

        if (transform.parent != null && transform.parent.name == "LightingExhibit")
            return transform.parent;

        // Fallback for a moved controller object.
        Transform current = transform;
        while (current != null)
        {
            if (current.name == "LightingExhibit")
                return current;
            current = current.parent;
        }

        return transform;
    }

    private void AutoWireReferences()
    {
        if (exhibitRoot == null)
            return;

        if (leftSpotLight == null)
            leftSpotLight = FindComponent<Light>("Lights/Left Spot Light");

        if (rightSpotLight == null)
            rightSpotLight = FindComponent<Light>("Lights/Right Spot Light");

        if (rearSpotLight == null)
            rearSpotLight = FindComponent<Light>("Lights/Rear Spot Light");

        if (subjectRoot == null)
            subjectRoot = exhibitRoot.Find("Environment/Fred_Exhibit1");

        if (buttonCanvas == null)
            buttonCanvas = exhibitRoot.Find("Controls/Canvas_Buttons");

        if (threePointButton == null)
            threePointButton = FindComponent<Button>("Controls/Canvas_Buttons/Butt_3point");

        if (highKeyButton == null)
            highKeyButton = FindComponent<Button>("Controls/Canvas_Buttons/Butt_HighKey");

        if (lowKeyButton == null)
            lowKeyButton = FindComponent<Button>("Controls/Canvas_Buttons/Butt_LowKey");

        if (horrorButton == null)
            horrorButton = FindComponent<Button>("Controls/Canvas_Buttons/Butt_Horror");

        if (threePointText == null)
            threePointText = FindComponent<TMP_Text>("Controls/Canvas_Buttons/Butt_3point/3Point_Text");

        if (highKeyText == null)
            highKeyText = FindComponent<TMP_Text>("Controls/Canvas_Buttons/Butt_HighKey/High_Text");

        if (lowKeyText == null)
            lowKeyText = FindComponent<TMP_Text>("Controls/Canvas_Buttons/Butt_LowKey/Low_Text");

        if (horrorText == null)
            horrorText = FindComponent<TMP_Text>("Controls/Canvas_Buttons/Butt_Horror/Horror_Text");
    }

    private T FindComponent<T>(string path) where T : Component
    {
        Transform found = exhibitRoot.Find(path);
        return found != null ? found.GetComponent<T>() : null;
    }

    private bool ValidateRequiredReferences()
    {
        bool valid = true;

        if (leftSpotLight == null)
        {
            Debug.LogError("Lighting Exhibit: Could not find 'Lights/Left Spot Light'.", this);
            valid = false;
        }

        if (rightSpotLight == null)
        {
            Debug.LogError("Lighting Exhibit: Could not find 'Lights/Right Spot Light'.", this);
            valid = false;
        }

        if (rearSpotLight == null)
        {
            Debug.LogError("Lighting Exhibit: Could not find 'Lights/Rear Spot Light'.", this);
            valid = false;
        }

        if (subjectRoot == null)
        {
            // The exhibit still works without this except for moving the horror key.
            Debug.LogWarning("Lighting Exhibit: Fred_Exhibit1 was not found. Horror mode will keep the key light in its normal position.", this);
        }

        return valid;
    }

    private void CacheDefaultLightTransforms()
    {
        keyDefaultPosition = leftSpotLight.transform.localPosition;
        keyDefaultRotation = leftSpotLight.transform.localRotation;

        fillDefaultPosition = rightSpotLight.transform.localPosition;
        fillDefaultRotation = rightSpotLight.transform.localRotation;

        rimDefaultPosition = rearSpotLight.transform.localPosition;
        rimDefaultRotation = rearSpotLight.transform.localRotation;
    }

    private void HookButtons()
    {
        if (threePointButton != null) threePointButton.onClick.AddListener(SetThreePoint);
        if (highKeyButton != null) highKeyButton.onClick.AddListener(SetHighKey);
        if (lowKeyButton != null) lowKeyButton.onClick.AddListener(SetLowKey);
        if (horrorButton != null) horrorButton.onClick.AddListener(SetHorror);
    }

    public void SetThreePoint()
    {
        if (!referencesValid) return;

        RestoreDefaultLightTransforms();

        ConfigureLight(
            leftSpotLight,
            enabled: true,
            intensity: 1.20f,
            range: 6.0f,
            outerAngle: 50f,
            innerAngle: 35f,
            temperature: 5200f,
            filterColor: Color.white,
            shadows: LightShadows.Soft,
            shadowStrength: 0.80f);

        ConfigureLight(
            rightSpotLight,
            enabled: true,
            intensity: 0.40f,
            range: 6.0f,
            outerAngle: 70f,
            innerAngle: 45f,
            temperature: 6000f,
            filterColor: Color.white,
            shadows: LightShadows.None);

        ConfigureLight(
            rearSpotLight,
            enabled: true,
            intensity: 0.80f,
            range: 6.0f,
            outerAngle: 40f,
            innerAngle: 25f,
            temperature: 6500f,
            filterColor: Color.white,
            shadows: LightShadows.None);

        ShowDescription(threePointText);
    }

    public void SetHighKey()
    {
        if (!referencesValid) return;

        RestoreDefaultLightTransforms();

        ConfigureLight(
            leftSpotLight,
            enabled: true,
            intensity: 1.00f,
            range: 6.0f,
            outerAngle: 55f,
            innerAngle: 38f,
            temperature: 5500f,
            filterColor: Color.white,
            shadows: LightShadows.Soft,
            shadowStrength: 0.55f);

        ConfigureLight(
            rightSpotLight,
            enabled: true,
            intensity: 0.80f,
            range: 6.0f,
            outerAngle: 75f,
            innerAngle: 50f,
            temperature: 5600f,
            filterColor: Color.white,
            shadows: LightShadows.None);

        ConfigureLight(
            rearSpotLight,
            enabled: true,
            intensity: 0.50f,
            range: 6.0f,
            outerAngle: 45f,
            innerAngle: 30f,
            temperature: 6000f,
            filterColor: Color.white,
            shadows: LightShadows.None);

        ShowDescription(highKeyText);
    }

    public void SetLowKey()
    {
        if (!referencesValid) return;

        RestoreDefaultLightTransforms();

        ConfigureLight(
            leftSpotLight,
            enabled: true,
            intensity: 1.15f,
            range: 6.0f,
            outerAngle: 40f,
            innerAngle: 28f,
            temperature: 4300f,
            filterColor: Color.white,
            shadows: LightShadows.Soft,
            shadowStrength: 0.90f);

        ConfigureLight(
            rightSpotLight,
            enabled: true,
            intensity: 0.08f,
            range: 5.0f,
            outerAngle: 55f,
            innerAngle: 35f,
            temperature: 5500f,
            filterColor: Color.white,
            shadows: LightShadows.None);

        ConfigureLight(
            rearSpotLight,
            enabled: true,
            intensity: 0.75f,
            range: 6.0f,
            outerAngle: 38f,
            innerAngle: 24f,
            temperature: 7000f,
            filterColor: Color.white,
            shadows: LightShadows.None);

        ShowDescription(lowKeyText);
    }

    public void SetHorror()
    {
        if (!referencesValid) return;

        RestoreDefaultLightTransforms();
        MoveKeyToHorrorUnderlightPosition();

        // Slight green filter + warm-ish temperature. Keep the tint subtle.
        ConfigureLight(
            leftSpotLight,
            enabled: true,
            intensity: 0.90f,
            range: 5.0f,
            outerAngle: 45f,
            innerAngle: 30f,
            temperature: 4000f,
            filterColor: new Color(0.80f, 1.00f, 0.80f),
            shadows: LightShadows.Soft,
            shadowStrength: 0.90f);

        // Remove fill so the upward key creates strong unnatural shadows.
        ConfigureLight(
            rightSpotLight,
            enabled: false,
            intensity: 0f,
            range: 5.0f,
            outerAngle: 60f,
            innerAngle: 40f,
            temperature: 5500f,
            filterColor: Color.white,
            shadows: LightShadows.None);

        // Cool rim keeps the subject separated from the background.
        ConfigureLight(
            rearSpotLight,
            enabled: true,
            intensity: 0.45f,
            range: 6.0f,
            outerAngle: 40f,
            innerAngle: 25f,
            temperature: 7500f,
            filterColor: new Color(0.85f, 0.92f, 1.00f),
            shadows: LightShadows.None);

        ShowDescription(horrorText);
    }

    private void ConfigureLight(
        Light light,
        bool enabled,
        float intensity,
        float range,
        float outerAngle,
        float innerAngle,
        float temperature,
        Color filterColor,
        LightShadows shadows,
        float shadowStrength = 1f)
    {
        light.enabled = enabled;
        light.type = LightType.Spot;
        light.intensity = intensity;
        light.range = range;
        light.spotAngle = outerAngle;
        light.innerSpotAngle = Mathf.Min(innerAngle, outerAngle);
        light.useColorTemperature = true;
        light.colorTemperature = temperature;
        light.color = filterColor;
        light.shadows = shadows;
        light.shadowStrength = shadowStrength;
    }

    private void RestoreDefaultLightTransforms()
    {
        leftSpotLight.transform.localPosition = keyDefaultPosition;
        leftSpotLight.transform.localRotation = keyDefaultRotation;

        rightSpotLight.transform.localPosition = fillDefaultPosition;
        rightSpotLight.transform.localRotation = fillDefaultRotation;

        rearSpotLight.transform.localPosition = rimDefaultPosition;
        rearSpotLight.transform.localRotation = rimDefaultRotation;
    }

    private void MoveKeyToHorrorUnderlightPosition()
    {
        if (subjectRoot == null)
            return;

        // Use the button panel as the "audience side" of the exhibit. This is
        // more reliable here than assuming the imported character's local
        // forward axis points toward the viewer.
        Vector3 audienceDirection;

        if (buttonCanvas != null)
        {
            audienceDirection = Vector3.ProjectOnPlane(
                buttonCanvas.position - subjectRoot.position,
                Vector3.up);
        }
        else
        {
            audienceDirection = Vector3.ProjectOnPlane(
                leftSpotLight.transform.position - subjectRoot.position,
                Vector3.up);
        }

        if (audienceDirection.sqrMagnitude < 0.001f)
            audienceDirection = Vector3.forward;
        else
            audienceDirection.Normalize();

        Vector3 faceTarget = subjectRoot.position + Vector3.up * faceHeight;
        Vector3 underlightPosition =
            subjectRoot.position +
            Vector3.up * horrorKeyHeight +
            audienceDirection * horrorKeyForwardDistance;

        leftSpotLight.transform.position = underlightPosition;
        leftSpotLight.transform.rotation = Quaternion.LookRotation(
            faceTarget - underlightPosition,
            Vector3.up);
    }

    private void ShowDescription(TMP_Text selected)
    {
        SetTextObjectActive(threePointText, selected == threePointText);
        SetTextObjectActive(highKeyText, selected == highKeyText);
        SetTextObjectActive(lowKeyText, selected == lowKeyText);
        SetTextObjectActive(horrorText, selected == horrorText);
    }

    private static void SetTextObjectActive(TMP_Text text, bool active)
    {
        if (text != null)
            text.gameObject.SetActive(active);
    }

    private void WritePresetDescriptions()
    {
        if (threePointText != null)
        {
            threePointText.text =
                "<b>THREE-POINT LIGHTING</b>\n" +
                "Key: 1.2 / 5200 K\n" +
                "Fill: 0.4 / 6000 K\n" +
                "Rim: 0.8 / 6500 K\n\n" +
                "The key creates form, the fill controls contrast, and the rim separates the subject from the background.";
        }

        if (highKeyText != null)
        {
            highKeyText.text =
                "<b>HIGH-KEY LIGHTING</b>\n" +
                "Key: 1.0 / Fill: 0.8 / Rim: 0.5\n\n" +
                "A strong fill reduces shadows and contrast. The result feels bright, even, clean, and often upbeat.";
        }

        if (lowKeyText != null)
        {
            lowKeyText.text =
                "<b>LOW-KEY LIGHTING</b>\n" +
                "Key: 1.15 / Fill: 0.08 / Rim: 0.75\n\n" +
                "Very little fill leaves deep shadows. The higher contrast creates a more dramatic or mysterious image.";
        }

        if (horrorText != null)
        {
            horrorText.text =
                "<b>HORROR / UNDERLIGHTING</b>\n" +
                "Key: below the face / Fill: off / Cool rim\n\n" +
                "Lighting upward creates unfamiliar shadows around the eyes, nose, and jaw, making the same subject feel unsettling.";
        }
    }
}

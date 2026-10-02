using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CompositionFredHeadAnimator : MonoBehaviour
{
    [Header("Fred")]
    [SerializeField] private Animator fredAnimator;
    [SerializeField] private Transform headBone;

    [Header("Buttons")]
    [SerializeField] private Button wideButton;
    [SerializeField] private Button mediumButton;
    [SerializeField] private Button closeUpButton;
    [SerializeField] private Button lowAngleButton;
    [SerializeField] private Button highAngleButton;

    [Header("Head Movement")]
    [SerializeField] private float lowAnglePitch = 22f;
    [SerializeField] private float highAnglePitch = -22f;
    [SerializeField] private float animationDuration = 0.55f;

    [Header("Quirkiness")]
    [SerializeField] private float overshootAmount = 6f;

    private Quaternion neutralRotation;
    private Quaternion targetRotation;

    private Coroutine animationRoutine;

    private void Awake()
    {
        FindHeadBone();

        if (headBone != null)
        {
            neutralRotation = headBone.localRotation;
            targetRotation = neutralRotation;
        }
    }

    private void Start()
    {
        if (wideButton != null)
            wideButton.onClick.AddListener(LookNeutral);

        if (mediumButton != null)
            mediumButton.onClick.AddListener(LookNeutral);

        if (closeUpButton != null)
            closeUpButton.onClick.AddListener(LookNeutral);

        if (lowAngleButton != null)
            lowAngleButton.onClick.AddListener(LookDown);

        if (highAngleButton != null)
            highAngleButton.onClick.AddListener(LookUp);
    }

    private void FindHeadBone()
    {
        if (headBone != null)
            return;

        if (fredAnimator != null &&
            fredAnimator.isHuman)
        {
            headBone =
                fredAnimator.GetBoneTransform(
                    HumanBodyBones.Head
                );
        }
    }

    public void LookNeutral()
    {
        AnimateTo(neutralRotation, 0f);
    }

    public void LookDown()
    {
        Quaternion rotation =
            neutralRotation *
            Quaternion.Euler(lowAnglePitch, 0f, 0f);

        AnimateTo(rotation, overshootAmount);
    }

    public void LookUp()
    {
        Quaternion rotation =
            neutralRotation *
            Quaternion.Euler(highAnglePitch, 0f, 0f);

        AnimateTo(rotation, -overshootAmount);
    }

    private void AnimateTo(
        Quaternion finalRotation,
        float overshoot)
    {
        if (headBone == null)
            return;

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine =
            StartCoroutine(
                AnimateHead(finalRotation, overshoot)
            );
    }

    private IEnumerator AnimateHead(
        Quaternion finalRotation,
        float overshoot)
    {
        Quaternion startingRotation =
            headBone.localRotation;

        Quaternion overshootRotation =
            finalRotation *
            Quaternion.Euler(overshoot, 0f, 0f);

        float firstDuration =
            animationDuration * 0.7f;

        float elapsed = 0f;

        while (elapsed < firstDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / firstDuration
            );

            t = Mathf.SmoothStep(0f, 1f, t);

            headBone.localRotation =
                Quaternion.Slerp(
                    startingRotation,
                    overshootRotation,
                    t
                );

            yield return null;
        }

        elapsed = 0f;

        float settleDuration =
            animationDuration * 0.3f;

        while (elapsed < settleDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / settleDuration
            );

            t = Mathf.SmoothStep(0f, 1f, t);

            headBone.localRotation =
                Quaternion.Slerp(
                    overshootRotation,
                    finalRotation,
                    t
                );

            yield return null;
        }

        headBone.localRotation = finalRotation;
        targetRotation = finalRotation;

        animationRoutine = null;
    }
}
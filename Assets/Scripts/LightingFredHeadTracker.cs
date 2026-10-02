using UnityEngine;
using UnityEngine.UI;

public class LightingFredHeadTracker : MonoBehaviour
{
    [Header("Fred")]
    [SerializeField] private Animator fredAnimator;
    [SerializeField] private Transform headBone;

    [Header("Player")]
    [Tooltip("Use CenterEyeAnchor.")]
    [SerializeField] private Transform playerHead;

    [Header("Trigger")]
    [SerializeField] private Button horrorButton;

    [Header("Tracking")]
    [SerializeField] private float turnSpeed = 2f;

    [Tooltip("Maximum left/right head turn.")]
    [SerializeField] private float maxYaw = 65f;

    [Tooltip("Maximum up/down head turn.")]
    [SerializeField] private float maxPitch = 30f;

    private Quaternion neutralLocalRotation;
    private Transform headParent;
    private bool trackingPlayer;

    private void Awake()
    {
        FindHeadBone();

        if (headBone != null)
        {
            neutralLocalRotation = headBone.localRotation;
            headParent = headBone.parent;
        }
    }

    private void Start()
    {
        if (horrorButton != null)
        {
            horrorButton.onClick.AddListener(BeginTracking);
        }
    }

    private void FindHeadBone()
    {
        if (headBone != null)
            return;

        if (fredAnimator != null && fredAnimator.isHuman)
        {
            headBone =
                fredAnimator.GetBoneTransform(HumanBodyBones.Head);
        }
    }

    public void BeginTracking()
    {
        trackingPlayer = true;
    }

    private void LateUpdate()
    {
        if (!trackingPlayer ||
            headBone == null ||
            headParent == null ||
            playerHead == null)
        {
            return;
        }

        Vector3 direction =
            playerHead.position - headBone.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Vector3 localDirection =
            headParent.InverseTransformDirection(
                direction.normalized
            );

        float yaw =
            Mathf.Atan2(
                localDirection.x,
                localDirection.z
            ) * Mathf.Rad2Deg;

        float pitch =
            -Mathf.Asin(
                Mathf.Clamp(
                    localDirection.y,
                    -1f,
                    1f
                )
            ) * Mathf.Rad2Deg;

        yaw = Mathf.Clamp(
            yaw,
            -maxYaw,
            maxYaw
        );

        pitch = Mathf.Clamp(
            pitch,
            -maxPitch,
            maxPitch
        );

        Quaternion desiredRotation =
            neutralLocalRotation *
            Quaternion.Euler(
                pitch,
                yaw,
                0f
            );

        headBone.localRotation =
            Quaternion.Slerp(
                headBone.localRotation,
                desiredRotation,
                turnSpeed * Time.deltaTime
            );
    }
}

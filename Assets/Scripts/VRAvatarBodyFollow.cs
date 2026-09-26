using UnityEngine;

public class VRAvatarBodyFollow : MonoBehaviour
{
    [Header("Tracking")]
    [SerializeField] private Transform playerHead;

    [Header("Rotation")]
    [Tooltip("Additional correction if the avatar model's forward direction is offset.")]
    [SerializeField] private float yawOffset = 0f;

    [Tooltip("How quickly the avatar body turns toward the headset.")]
    [SerializeField] private float turnSpeed = 720f;

    [Tooltip("If disabled, the avatar instantly matches the headset yaw.")]
    [SerializeField] private bool smoothRotation = true;

    private void Update()
    {
        if (playerHead == null)
            return;

        // Get the direction the headset is facing,
        // ignoring looking up/down.
        Vector3 headForward =
            Vector3.ProjectOnPlane(
                playerHead.forward,
                Vector3.up
            );

        if (headForward.sqrMagnitude < 0.001f)
            return;

        headForward.Normalize();

        Quaternion targetRotation =
            Quaternion.LookRotation(
                headForward,
                Vector3.up
            );

        // Allow correction for avatars whose imported
        // "forward" isn't Unity's +Z direction.
        targetRotation *= Quaternion.Euler(
            0f,
            yawOffset,
            0f
        );

        if (smoothRotation)
        {
            transform.rotation =
                Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime
                );
        }
        else
        {
            transform.rotation = targetRotation;
        }
    }
}

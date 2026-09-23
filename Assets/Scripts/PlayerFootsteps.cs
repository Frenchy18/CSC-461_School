using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("The object whose world position changes when the player locomotes. For this project, use PlayerController.")]
    public Transform movementRoot;

    [Tooltip("Distance travelled between footsteps. Smaller values = faster steps.")]
    [Min(0.1f)]
    public float stepDistance = 0.72f;

    [Tooltip("Ignore tiny tracking/drift movement below this speed.")]
    [Min(0f)]
    public float minimumSpeed = 0.12f;

    [Tooltip("If the player moves farther than this in one frame, treat it as a teleport and do not play a step.")]
    [Min(0.1f)]
    public float teleportDistance = 1.25f;

    [Header("Ground Detection")]
    [Tooltip("Layers that can provide footsteps. Ground + Default is a good starting point for this project.")]
    public LayerMask groundMask = ~0;

    [Tooltip("How far above movementRoot to begin the downward raycast.")]
    [Min(0f)]
    public float rayStartHeight = 1.0f;

    [Tooltip("How far downward to search for a walkable surface.")]
    [Min(0.1f)]
    public float rayDistance = 3.5f;

    [Header("Fallback")]
    [Tooltip("Optional surface used if the ground collider has no FootstepSurface component.")]
    public FootstepSurface defaultSurface;

    [Header("Debug")]
    public bool drawGroundRay = false;

    private AudioSource _audioSource;
    private Vector3 _lastPosition;
    private float _distanceSinceStep;
    private bool _initialized;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.spatialBlend = 0f;

        if (movementRoot == null)
            movementRoot = transform;
    }

    private void OnEnable()
    {
        ResetTracking();
    }

    private void LateUpdate()
    {
        if (movementRoot == null)
            return;

        Vector3 currentPosition = movementRoot.position;

        if (!_initialized)
        {
            _lastPosition = currentPosition;
            _initialized = true;
            return;
        }

        Vector3 delta = currentPosition - _lastPosition;
        _lastPosition = currentPosition;

        Vector2 horizontalDelta = new Vector2(delta.x, delta.z);
        float frameDistance = horizontalDelta.magnitude;

        if (frameDistance > teleportDistance)
        {
            _distanceSinceStep = 0f;
            return;
        }

        float speed = Time.deltaTime > 0f ? frameDistance / Time.deltaTime : 0f;

        if (speed < minimumSpeed)
            return;

        if (!TryFindSurface(out FootstepSurface surface))
            return;

        _distanceSinceStep += frameDistance;

        if (_distanceSinceStep < stepDistance)
            return;

        _distanceSinceStep %= stepDistance;
        PlayStep(surface);
    }

    private bool TryFindSurface(out FootstepSurface surface)
    {
        surface = null;

        Vector3 origin = movementRoot.position + Vector3.up * rayStartHeight;
        float totalDistance = rayStartHeight + rayDistance;

        if (drawGroundRay)
            Debug.DrawRay(origin, Vector3.down * totalDistance, Color.yellow);

        if (!Physics.Raycast(
                origin,
                Vector3.down,
                out RaycastHit hit,
                totalDistance,
                groundMask,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        surface = hit.collider.GetComponent<FootstepSurface>();

        if (surface == null)
            surface = hit.collider.GetComponentInParent<FootstepSurface>();

        if (surface == null)
            surface = defaultSurface;

        return surface != null;
    }

    private void PlayStep(FootstepSurface surface)
    {
        if (!surface.TryGetFootstep(out AudioClip clip, out float volume, out float pitch))
            return;

        _audioSource.pitch = pitch;
        _audioSource.PlayOneShot(clip, volume);
    }

    public void ResetTracking()
    {
        _distanceSinceStep = 0f;
        _initialized = false;

        if (movementRoot != null)
            _lastPosition = movementRoot.position;
    }
}

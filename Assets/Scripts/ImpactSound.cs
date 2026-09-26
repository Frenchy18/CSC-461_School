using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ImpactSound : MonoBehaviour
{
    [Header("Impact Sounds")]
    [SerializeField] private AudioClip[] impactClips;

    [Header("Impact Strength")]
    [SerializeField] private float minimumImpactSpeed = 1.0f;
    [SerializeField] private float maximumImpactSpeed = 7.0f;

    [Header("Sound Variation")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.9f, 1.1f);
    [SerializeField] private float maxVolume = 0.8f;

    [Header("Spam Prevention")]
    [SerializeField] private float cooldown = 0.08f;

    private AudioSource audioSource;
    private float lastImpactTime;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (impactClips == null || impactClips.Length == 0)
            return;

        if (Time.time - lastImpactTime < cooldown)
            return;

        float impactSpeed = collision.relativeVelocity.magnitude;

        if (impactSpeed < minimumImpactSpeed)
            return;

        float strength = Mathf.InverseLerp(
            minimumImpactSpeed,
            maximumImpactSpeed,
            impactSpeed
        );

        float volume = strength * maxVolume;

        AudioClip clip = impactClips[
            Random.Range(0, impactClips.Length)
        ];

        audioSource.pitch = Random.Range(
            pitchRange.x,
            pitchRange.y
        );

        audioSource.PlayOneShot(clip, volume);

        lastImpactTime = Time.time;
    }
}
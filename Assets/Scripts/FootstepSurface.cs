using UnityEngine;

public class FootstepSurface : MonoBehaviour
{
    [Header("Footstep Clips")]
    [Tooltip("Add several variations of the same surface for less repetition.")]
    public AudioClip[] clips;

    [Header("Sound Variation")]
    [Range(0f, 1f)]
    public float volume = 0.8f;

    [Min(0.01f)]
    public float minPitch = 0.94f;

    [Min(0.01f)]
    public float maxPitch = 1.06f;

    private int _lastClipIndex = -1;

    public bool TryGetFootstep(out AudioClip clip, out float selectedVolume, out float selectedPitch)
    {
        clip = null;
        selectedVolume = volume;
        selectedPitch = Random.Range(
            Mathf.Min(minPitch, maxPitch),
            Mathf.Max(minPitch, maxPitch)
        );

        if (clips == null || clips.Length == 0)
            return false;

        if (clips.Length == 1)
        {
            _lastClipIndex = 0;
            clip = clips[0];
            return clip != null;
        }

        int index = Random.Range(0, clips.Length);

        if (index == _lastClipIndex)
            index = (index + Random.Range(1, clips.Length)) % clips.Length;

        _lastClipIndex = index;
        clip = clips[index];

        return clip != null;
    }
}
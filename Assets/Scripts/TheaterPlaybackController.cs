using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class TheaterPlaybackController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform playerRoot;

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private bool restartFromBeginning = true;
    [SerializeField] private bool clearScreenWhenInactive = true;

    [Header("Projector Flicker")]
    [SerializeField] private Vector2 flickerInterval = new Vector2(0.06f, 0.16f);
    [SerializeField] private Vector2 intensityMultiplier = new Vector2(0.82f, 1.08f);

    [Header("Projector")]
    [SerializeField] private Light projectorLight;
    [SerializeField] private AudioSource projectorAudio;

    [Range(0f, 1f)]
    [SerializeField] private float dropoutChance = 0.04f;

    [SerializeField] private Vector2 dropoutDuration = new Vector2(0.03f, 0.06f);

    private readonly HashSet<Collider> playerColliders = new HashSet<Collider>();

    private Coroutine flickerCoroutine;
    private float baseProjectorIntensity;
    private bool playerInside;
    private bool preparationRequested;

    private void Awake()
    {
        if (projectorLight != null)
        {
            baseProjectorIntensity = projectorLight.intensity;
            projectorLight.enabled = false;
        }

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;

            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.errorReceived += OnVideoError;

            // Prepare now so playback can begin immediately
            // when the player eventually enters the theater.
            preparationRequested = true;
            videoPlayer.Prepare();
        }

        if (clearScreenWhenInactive)
            ClearVideoScreen();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other))
            return;

        playerColliders.Add(other);

        // Only activate once when the first player collider enters.
        if (playerColliders.Count == 1)
            EnterTheater();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other))
            return;

        playerColliders.Remove(other);

        // Don't stop the movie just because a hand/controller
        // temporarily leaves the trigger.
        if (playerColliders.Count == 0)
            ExitTheater();
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (playerRoot == null)
            return false;

        Transform otherTransform = other.transform;

        return otherTransform == playerRoot ||
               otherTransform.IsChildOf(playerRoot);
    }

    private void EnterTheater()
    {
        playerInside = true;

        StartProjector();

        if (videoPlayer == null)
            return;

        if (videoPlayer.isPrepared)
        {
            StartVideo();
        }
        else if (!preparationRequested)
        {
            preparationRequested = true;
            videoPlayer.Prepare();
        }
    }

    private void ExitTheater()
    {
        playerInside = false;

        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            // Pause instead of Stop so Unity keeps the video prepared.
            // This makes the next entrance start much faster.
            videoPlayer.Pause();
        }

        if (projectorAudio != null)
            projectorAudio.Stop();

        StopProjector();

        if (clearScreenWhenInactive)
            ClearVideoScreen();
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        preparationRequested = false;

        if (playerInside)
        {
            StartVideo();
        }
        else if (clearScreenWhenInactive)
        {
            ClearVideoScreen();
        }
    }

    private void StartVideo()
    {
        if (videoPlayer == null || !videoPlayer.isPrepared)
            return;

        if (restartFromBeginning && videoPlayer.canSetTime)
            videoPlayer.time = 0.0;

        videoPlayer.Play();

        if (projectorAudio != null &&
            !projectorAudio.isPlaying)
        {
            projectorAudio.Play();
        }
    }

    private void StartProjector()
    {
        if (projectorLight == null)
            return;

        projectorLight.enabled = true;
        projectorLight.intensity = baseProjectorIntensity;

        if (flickerCoroutine != null)
            StopCoroutine(flickerCoroutine);

        flickerCoroutine = StartCoroutine(ProjectorFlicker());
    }

    private void StopProjector()
    {
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }

        if (projectorLight != null)
        {
            projectorLight.intensity = baseProjectorIntensity;
            projectorLight.enabled = false;
        }
    }

    private IEnumerator ProjectorFlicker()
    {
        while (playerInside)
        {
            projectorLight.enabled = true;

            projectorLight.intensity =
                baseProjectorIntensity *
                Random.Range(intensityMultiplier.x, intensityMultiplier.y);

            // Rare, very short projector dropout.
            if (Random.value < dropoutChance)
            {
                projectorLight.enabled = false;

                yield return new WaitForSeconds(
                    Random.Range(dropoutDuration.x, dropoutDuration.y)
                );

                if (!playerInside)
                    yield break;

                projectorLight.enabled = true;
            }

            yield return new WaitForSeconds(
                Random.Range(flickerInterval.x, flickerInterval.y)
            );
        }
    }

    private void ClearVideoScreen()
    {
        if (videoPlayer == null || videoPlayer.targetTexture == null)
            return;

        RenderTexture target = videoPlayer.targetTexture;

        if (!target.IsCreated())
            target.Create();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;

        GL.Clear(false, true, Color.black);

        RenderTexture.active = previous;
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogError($"Theater video error: {message}", this);
    }

    private void OnDisable()
    {
        playerColliders.Clear();
        playerInside = false;

        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Pause();

        StopProjector();

        if (projectorAudio != null)
            projectorAudio.Stop();
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.errorReceived -= OnVideoError;
        }
    }
}

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class IntermittentLightFlicker : MonoBehaviour
{
    [Header("Flicker Bursts")]
    [SerializeField] private Vector2 timeBetweenBursts =
        new Vector2(1.5f, 4.0f);

    [SerializeField] private Vector2 flickerDuration =
        new Vector2(0.035f, 0.11f);

    [SerializeField] private Vector2 intensityMultiplier =
        new Vector2(0.35f, 1.1f);

    [SerializeField] private Vector2Int flickersPerBurst =
        new Vector2Int(2, 6);

    [Header("Occasional Blackout")]
    [Range(0f, 1f)]
    [SerializeField] private float blackoutChance = 0.25f;

    [SerializeField] private Vector2 blackoutDuration =
        new Vector2(0.25f, 1.5f);

    private Light lightSource;
    private float normalIntensity;

    private void Awake()
    {
        lightSource = GetComponent<Light>();
        normalIntensity = lightSource.intensity;
    }

    private void OnEnable()
    {
        StartCoroutine(FlickerLoop());
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        if (lightSource != null)
        {
            lightSource.enabled = true;
            lightSource.intensity = normalIntensity;
        }
    }

    private IEnumerator FlickerLoop()
    {
        while (true)
        {
            // Spend most of the time behaving normally.
            lightSource.enabled = true;
            lightSource.intensity = normalIntensity;

            yield return new WaitForSeconds(
                Random.Range(timeBetweenBursts.x, timeBetweenBursts.y)
            );

            int flickerCount = Random.Range(
                flickersPerBurst.x,
                flickersPerBurst.y + 1
            );

            // Short unstable flicker burst.
            for (int i = 0; i < flickerCount; i++)
            {
                lightSource.intensity =
                    normalIntensity *
                    Random.Range(
                        intensityMultiplier.x,
                        intensityMultiplier.y
                    );

                yield return new WaitForSeconds(
                    Random.Range(
                        flickerDuration.x,
                        flickerDuration.y
                    )
                );

                lightSource.intensity = normalIntensity;

                yield return new WaitForSeconds(
                    Random.Range(0.025f, 0.09f)
                );
            }

            // Sometimes the light completely dies.
            if (Random.value <= blackoutChance)
            {
                lightSource.enabled = false;

                yield return new WaitForSeconds(
                    Random.Range(
                        blackoutDuration.x,
                        blackoutDuration.y
                    )
                );

                lightSource.enabled = true;
                lightSource.intensity = normalIntensity;
            }
        }
    }
}
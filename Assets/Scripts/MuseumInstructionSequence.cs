using System.Collections;
using TMPro;
using UnityEngine;

public class MuseumInstructionSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text instructionText;

    [Header("Timing")]
    [SerializeField] private float messageDuration = 4f;
    [SerializeField] private float fadeDuration = 0.75f;

    [Header("Ending")]
    [SerializeField] private bool hideAfterFinalMessage = true;
    [SerializeField] private float finalMessageDuration = 5f;

    private readonly string[] messages =
    {
        "Welcome to the Cinematography and Lighting Museum",

        "Follow the lighted path anyway you choose",

        "Your watch will show you how many exhibits are left.\n" +
        "Check for interactable objects along the way",

        "Enjoy the ambiance",

        "No you can't go in the office"
    };

    private void Start()
    {
        if (instructionText == null)
        {
            Debug.LogWarning(
                "MuseumInstructionSequence: No instruction text assigned.",
                this
            );

            return;
        }

        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        instructionText.text = messages[0];
        SetTextAlpha(1f);

        for (int i = 0; i < messages.Length; i++)
        {
            float holdTime =
                i == messages.Length - 1
                    ? finalMessageDuration
                    : messageDuration;

            yield return new WaitForSeconds(holdTime);

            // If this is the final message, optionally fade away.
            if (i == messages.Length - 1)
            {
                if (hideAfterFinalMessage)
                {
                    yield return FadeText(1f, 0f);
                    gameObject.SetActive(false);
                }

                yield break;
            }

            // Fade current message out.
            yield return FadeText(1f, 0f);

            // Change the message while invisible.
            instructionText.text = messages[i + 1];

            // Fade the new message in.
            yield return FadeText(0f, 1f);
        }
    }

    private IEnumerator FadeText(float from, float to)
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                fadeDuration <= 0f
                    ? 1f
                    : elapsed / fadeDuration;

            SetTextAlpha(Mathf.Lerp(from, to, t));

            yield return null;
        }

        SetTextAlpha(to);
    }

    private void SetTextAlpha(float alpha)
    {
        Color color = instructionText.color;
        color.a = alpha;
        instructionText.color = color;
    }
}

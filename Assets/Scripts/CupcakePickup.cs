using System.Collections;
using UnityEngine;
using Oculus.Interaction;

public class CupcakePickup : MonoBehaviour
{
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private Rigidbody rb;

    private bool hasBeenTaken;

    private void OnEnable()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised += HandlePointerEvent;
    }

    private void OnDisable()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised -= HandlePointerEvent;
    }

    private void HandlePointerEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Select && !hasBeenTaken)
        {
            hasBeenTaken = true;

            // Remove it from Chica's hand while preserving
            // its exact world position.
            transform.SetParent(null, true);
        }

        if (evt.Type == PointerEventType.Unselect && hasBeenTaken)
        {
            StartCoroutine(EnablePhysicsAfterRelease());
        }
    }

    private IEnumerator EnablePhysicsAfterRelease()
    {
        // Let Interaction SDK finish the release first.
        yield return null;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }
}

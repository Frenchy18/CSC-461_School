using UnityEngine;
using Oculus.Interaction;

public class HeldPropRelease : MonoBehaviour
{
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private Rigidbody rb;

    private bool hasBeenTaken = false;

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
        if (hasBeenTaken)
            return;

        if (evt.Type == PointerEventType.Select)
        {
            hasBeenTaken = true;

            transform.SetParent(null, true);

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

public class ProximityLightToggle : MonoBehaviour
{
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Light[] controlledLights;

    private readonly HashSet<Collider> playerColliders = new();

    private void Awake()
    {
        SetLights(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerColliders.Add(other);
        SetLights(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerColliders.Remove(other);

        if (playerColliders.Count == 0)
            SetLights(false);
    }

    private bool IsPlayer(Collider other)
    {
        if (playerRoot == null)
            return false;

        return other.transform.root == playerRoot.root;
    }

    private void SetLights(bool enabled)
    {
        foreach (Light light in controlledLights)
        {
            if (light != null)
                light.enabled = enabled;
        }
    }
}
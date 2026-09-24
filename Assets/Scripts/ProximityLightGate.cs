using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(1000)]
public class ProximityLightGate : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform playerRoot;

    [Header("Lights controlled by this exhibit")]
    [SerializeField] private Light[] controlledLights;

    private bool[] savedStates;
    private readonly HashSet<Collider> playerColliders = new();

    private void Start()
    {
        savedStates = new bool[controlledLights.Length];

        // Let the LightingExhibitController establish its starting mode first,
        // then remember that configuration.
        SaveCurrentState();

        // Player begins outside the exhibit, so shut the exhibit lights off.
        SetAllLights(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        bool wasOutside = playerColliders.Count == 0;

        playerColliders.Add(other);

        // Only restore once when the player first enters.
        if (wasOutside)
        {
            RestoreSavedState();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerColliders.Remove(other);

        // VR rigs can have multiple colliders, so don't shut the lights off
        // until every player collider has left the trigger.
        if (playerColliders.Count == 0)
        {
            SaveCurrentState();
            SetAllLights(false);
        }
    }

    private bool IsPlayer(Collider other)
    {
        if (playerRoot == null)
            return false;

        return other.transform == playerRoot ||
               other.transform.IsChildOf(playerRoot);
    }

    private void SaveCurrentState()
    {
        for (int i = 0; i < controlledLights.Length; i++)
        {
            if (controlledLights[i] != null)
                savedStates[i] = controlledLights[i].enabled;
        }
    }

    private void RestoreSavedState()
    {
        for (int i = 0; i < controlledLights.Length; i++)
        {
            if (controlledLights[i] != null)
                controlledLights[i].enabled = savedStates[i];
        }
    }

    private void SetAllLights(bool state)
    {
        foreach (Light light in controlledLights)
        {
            if (light != null)
                light.enabled = state;
        }
    }
}

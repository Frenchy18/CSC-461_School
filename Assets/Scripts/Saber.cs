using UnityEngine;

public class Saber : MonoBehaviour
{
    // Stores the saber's previous frame position to calculate slicing direction
    private Vector3 previousPos;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit;
        // Cast a Ray forward from the saber's current position, up to 1 unit in distance
        if (Physics.Raycast(transform.position, transform.forward, out hit, 1))
        {
            // Check if the object hit has the tag "moving" (only slice valid objects)
            if (hit.transform.tag == "Moving")
            {
                // Calculate the angle between the saber movement direction and the object's up direction
                // A large angle (>130*) implies the saber crossed through the object with a slicing motion.
                if (Vector3.Angle(transform.position - previousPos, hit.transform.up) > 130)
                {
                    // Destroy the sliced object
                    Destroy(hit.transform.gameObject);
                }
            }
        }
        // Update the previous position for next grame comparision
        previousPos = transform.position;
    }
}

using UnityEngine;

public class ColorChanger : MonoBehaviour
{
    private Material objectMaterial;

    public Color colorA = Color.red;
    public Color colorB = Color.blue;

    void Start()
    {
        objectMaterial = GetComponent<Renderer>().material;
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.time) + 1f) / 2f;
        objectMaterial.color = Color.Lerp(colorA, colorB, t);
    }
}
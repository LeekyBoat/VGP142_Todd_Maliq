using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SimplePickups : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("Trigger entered by: " + other.gameObject.name + " with tag: " + other.tag);
        //if (!other.CompareTag("Player")) return;

        if (CompareTag("Box"))
        {
            Debug.LogWarning("This is a box");
        }

        if (CompareTag("Sphere"))
        {
            Debug.LogWarning("This is a sphere");
        }

        Destroy(gameObject);
    }
}

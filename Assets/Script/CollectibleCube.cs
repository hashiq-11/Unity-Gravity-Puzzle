using UnityEngine;

public class CollectibleCube : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 60f;

    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Cube Collected!");

            Destroy(gameObject);
        }
    }
}
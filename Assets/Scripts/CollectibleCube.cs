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
        // Security check: Ensure enemies or physics objects can't trigger the collection
        if (other.CompareTag("Player"))
        {
            // Report the successful collection to the central Brain before destroying the object
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCube();
            }

            Debug.Log("Cube Collected!");
            Destroy(gameObject);
        }
    }
}
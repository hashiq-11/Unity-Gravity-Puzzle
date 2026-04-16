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
        // Standard tag check to make sure only the player can pick this up
        if (other.CompareTag("Player"))
        {
            // Report the collection to our central manager before we disappear
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCube();
            }

            Debug.Log("Cube Collected!");
            Destroy(gameObject);
        }
    }
}
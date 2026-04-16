using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target;
    public float maxDistance = 5.0f;
    public float sensitivity = 3.0f;

    [Header("Clamping")]
    public float minPitch = -30f;
    public float maxPitch = 60f;

    [Header("Collision & Smoothing")]
    public LayerMask collisionLayer;
    public float distanceSmoothSpeed = 10f;

    private float currentYaw = 0f;
    private float currentPitch = 0f;
    private Vector3 smoothedUp = Vector3.up;
    private float currentDistance;

    void Start()
    {
        // Lock the mouse for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (target != null) smoothedUp = target.up;
        currentDistance = maxDistance;
    }

    void LateUpdate()
    {
        if (!target) return;

        // Gather mouse input for rotating the view
        currentYaw += Input.GetAxis("Mouse X") * sensitivity;
        currentPitch -= Input.GetAxis("Mouse Y") * sensitivity;
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

        // This is important: We smoothly transition the camera's "up" vector 
        // to match the character's new gravity orientation.
        smoothedUp = Vector3.Slerp(smoothedUp, target.up, 20f * Time.deltaTime).normalized;

        Quaternion gravityAlignment = Quaternion.FromToRotation(Vector3.up, smoothedUp);
        Quaternion mouseRotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        Quaternion finalRotation = gravityAlignment * mouseRotation;

        // Offset the camera look-at point so it's around the character's head height
        Vector3 targetHeadPosition = target.position + (smoothedUp * 1.5f);
        Vector3 directionBackward = finalRotation * Vector3.back;

        float targetDistance = maxDistance;

        // Collision check: If a wall is between the player and the camera, 
        // pull the camera forward to avoid clipping.
        if (Physics.Linecast(targetHeadPosition, targetHeadPosition + (directionBackward * maxDistance), out RaycastHit hit, collisionLayer))
        {
            targetDistance = hit.distance - 0.15f;
        }

        // Smoothly zoom the camera in or out based on the collision result
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, distanceSmoothSpeed * Time.deltaTime);

        transform.position = targetHeadPosition + (directionBackward * currentDistance);
        transform.rotation = finalRotation;
    }
}
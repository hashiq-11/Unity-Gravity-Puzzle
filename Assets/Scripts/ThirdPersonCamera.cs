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

    private void Start()
    {
        // Lock the cursor for active gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Hook into the Game Over event to freeze camera rotation
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += DisableCameraInput;
        }

        if (target != null) smoothedUp = target.up;
        currentDistance = maxDistance;
    }

    private void OnDestroy()
    {
        // Clean up event listeners to prevent memory leaks
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= DisableCameraInput;
        }
    }

    private void DisableCameraInput(string message, bool isWin)
    {
        // Disabling the script stops LateUpdate, freeing the mouse for the UI
        this.enabled = false;

        // Final safety check to ensure the cursor is released
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void LateUpdate()
    {
        if (!target) return;

        // We return early to ignore all mouse movement and math calculations.
        if (Time.timeScale == 0) return;

        // Get raw mouse input and prevent the camera from flipping upside down
        currentYaw += Input.GetAxis("Mouse X") * sensitivity;
        currentPitch -= Input.GetAxis("Mouse Y") * sensitivity;
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

        // Smoothly transition the camera's 'up' direction during gravity shifts
        smoothedUp = Vector3.Slerp(smoothedUp, target.up, 20f * Time.deltaTime).normalized;

        Quaternion gravityAlignment = Quaternion.FromToRotation(Vector3.up, smoothedUp);
        Quaternion mouseRotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        Quaternion finalRotation = gravityAlignment * mouseRotation;

        // Find the ideal camera position behind the player's head
        Vector3 targetHeadPosition = target.position + (smoothedUp * 1.5f);
        Vector3 directionBackward = finalRotation * Vector3.back;

        float targetDistance = maxDistance;

        // Camera Collision: If a wall blocks the view, pull the camera closer
        if (Physics.Linecast(targetHeadPosition, targetHeadPosition + (directionBackward * maxDistance), out RaycastHit hit, collisionLayer))
        {
            targetDistance = hit.distance - 0.15f;
        }

        // Smoothly zoom in/out when hitting walls
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, distanceSmoothSpeed * Time.deltaTime);

        // Apply the final calculations
        transform.position = targetHeadPosition + (directionBackward * currentDistance);
        transform.rotation = finalRotation;
    }

    public void AlignBehindPlayer()
    {
        // 1. Figure out where "forward" is for the player, relative to the current gravity
        Quaternion gravityAlignment = Quaternion.FromToRotation(Vector3.up, smoothedUp);
        Vector3 localForward = Quaternion.Inverse(gravityAlignment) * target.forward;

        // 2. Calculate the exact orbital angle (Yaw) needed to put the camera directly behind their back
        currentYaw = Mathf.Atan2(localForward.x, localForward.z) * Mathf.Rad2Deg;

        // 3. Snap the vertical angle (Pitch) so we are looking slightly down at the player
        currentPitch = 15f;
    }
}
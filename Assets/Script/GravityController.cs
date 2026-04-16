using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GravityController : MonoBehaviour
{
    [Header("Gravity Settings")]
    public float gravityForce = 15f;
    public float rotationSpeed = 8f;

    [Header("Hologram Settings")]
    public Transform hologramTransform;
    public Transform mainCameraTransform;

    [Tooltip("Adjust this in the Inspector to slide the hologram up or down to fix model pivot offsets.")]
    public float hologramHeightOffset = 1.0f;

    public Vector3 CurrentGravityDir { get; private set; } = Vector3.down;
    private Vector3 targetGravityDir = Vector3.down;

    private bool isPreviewing = false;
    private bool isTransitioning = false;

    private void Start()
    {
        // Keep the hologram hidden until the player actively starts picking a new direction
        if (hologramTransform != null) hologramTransform.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Lock player input while the world is spinning to prevent glitchy state changes
        if (isTransitioning) return;

        HandleGravitySelection();
        HandleGravityConfirmation();
    }

    private void HandleGravitySelection()
    {
        Vector3 inputDir = Vector3.zero;

        // Map arrow keys to camera-relative directions so the controls always feel intuitive
        if (Input.GetKeyDown(KeyCode.UpArrow)) inputDir = mainCameraTransform.forward;
        else if (Input.GetKeyDown(KeyCode.DownArrow)) inputDir = -mainCameraTransform.forward;
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) inputDir = -mainCameraTransform.right;
        else if (Input.GetKeyDown(KeyCode.RightArrow)) inputDir = mainCameraTransform.right;

        if (inputDir.sqrMagnitude < 0.1f) return;

        // Snap the user's camera-relative input to the nearest absolute 3D world axis
        targetGravityDir = GetNearestAxis(inputDir);
        isPreviewing = true;

        if (hologramTransform != null)
        {
            hologramTransform.gameObject.SetActive(true);

            // --- Advanced Raycast Positioning ---
            // We shoot the ray from "chest height" and slightly forward to prevent the ray 
            // from hitting the floor we are standing on or the player's own collider.
            Vector3 chestOffset = transform.up * 1.0f;
            Vector3 forwardOffset = targetGravityDir * 0.8f;
            Vector3 rayOrigin = transform.position + chestOffset + forwardOffset;

            if (Physics.Raycast(rayOrigin, targetGravityDir, out RaycastHit hit, 50f))
            {
                // Place the hologram on the target surface.
                // We subtract chestOffset to drop the feet to the floor, then add hologramHeightOffset
                // so the developer can fine-tune the exact placement in the Inspector to avoid clipping.
                hologramTransform.position = hit.point + (hit.normal * 0.05f) - chestOffset + (transform.up * hologramHeightOffset);
            }
            else
            {
                // Fallback: If shooting into the void, spawn at player location
                hologramTransform.position = transform.position;
            }

            // --- Hologram Orientation ---
            // Orient the hologram so its "up" completely opposes the new gravity pull
            Vector3 holoUp = -targetGravityDir;

            // Align it to face exactly where the camera is looking
            Vector3 holoForward = Vector3.ProjectOnPlane(mainCameraTransform.forward, holoUp).normalized;

            // Failsafe: Prevent mathematical errors if looking straight up or down
            if (holoForward.sqrMagnitude < 0.1f)
                holoForward = Vector3.ProjectOnPlane(mainCameraTransform.up, holoUp).normalized;

            hologramTransform.rotation = Quaternion.LookRotation(holoForward, holoUp);
        }
    }

    private void HandleGravityConfirmation()
    {
        // Commit to the shift
        if (isPreviewing && Input.GetKeyDown(KeyCode.Return))
        {
            CurrentGravityDir = targetGravityDir;
            isPreviewing = false;

            if (hologramTransform != null) hologramTransform.gameObject.SetActive(false);

            StartCoroutine(TransitionGravityRotation());
        }

        // Cancel the shift
        if (isPreviewing && Input.GetKeyDown(KeyCode.Escape))
        {
            isPreviewing = false;
            if (hologramTransform != null) hologramTransform.gameObject.SetActive(false);
        }
    }

    private IEnumerator TransitionGravityRotation()
    {
        isTransitioning = true;

        // Temporarily disable physics and movement to ensure a completely smooth rotation
        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null) pm.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Calculate the exact rotation needed to stand on the new surface
        Vector3 targetUp = -CurrentGravityDir;
        Vector3 targetForward = Vector3.ProjectOnPlane(mainCameraTransform.forward, targetUp).normalized;

        if (targetForward.sqrMagnitude < 0.1f)
            targetForward = Vector3.ProjectOnPlane(mainCameraTransform.up, targetUp).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(targetForward, targetUp);
        float timeOut = 0f;

        // Smoothly interpolate the rotation (with a 2-second safety timeout to prevent soft-locks)
        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            timeOut += Time.fixedDeltaTime;
            if (timeOut > 2.0f) break;

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * 50f * Time.fixedDeltaTime
            );

            yield return new WaitForFixedUpdate();
        }

        // Snap to the exact final rotation for mathematical precision
        transform.rotation = targetRotation;

        // --- UX Polish: Camera Auto-Alignment ---
        // Automatically swing the camera directly behind the player's back so they don't 
        // have to manually readjust their mouse after every gravity shift.
        ThirdPersonCamera cam = mainCameraTransform.GetComponent<ThirdPersonCamera>();
        if (cam != null)
        {
            cam.AlignBehindPlayer();
        }

        // Restore physics control
        if (rb != null) rb.isKinematic = false;
        if (pm != null) pm.enabled = true;

        isTransitioning = false;
    }

    private Vector3 GetNearestAxis(Vector3 dir)
    {
        Vector3 nearest = Vector3.zero;
        float maxDot = -1f;

        Vector3[] axes = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

        // Use the Dot Product to mathematically find which absolute world axis perfectly matches the input
        foreach (Vector3 axis in axes)
        {
            float dot = Vector3.Dot(dir.normalized, axis);
            if (dot > maxDot)
            {
                maxDot = dot;
                nearest = axis;
            }
        }
        return nearest;
    }
}
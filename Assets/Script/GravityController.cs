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

    public Vector3 CurrentGravityDir { get; private set; } = Vector3.down;
    private Vector3 targetGravityDir = Vector3.down;

    private bool isPreviewing = false;
    private bool isTransitioning = false;

    private void Start()
    {
        if (hologramTransform != null) hologramTransform.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (isTransitioning) return;
        HandleGravitySelection();
        HandleGravityConfirmation();
    }

    private void HandleGravitySelection()
    {
        Vector3 inputDir = Vector3.zero;

        if (Input.GetKeyDown(KeyCode.UpArrow)) inputDir = mainCameraTransform.forward;
        else if (Input.GetKeyDown(KeyCode.DownArrow)) inputDir = -mainCameraTransform.forward;
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) inputDir = -mainCameraTransform.right;
        else if (Input.GetKeyDown(KeyCode.RightArrow)) inputDir = mainCameraTransform.right;

        if (inputDir.sqrMagnitude < 0.1f) return;

        targetGravityDir = GetNearestAxis(inputDir);
        isPreviewing = true;

        if (hologramTransform != null)
        {
            hologramTransform.gameObject.SetActive(true);
            hologramTransform.position = transform.position;

            Vector3 holoUp = -targetGravityDir;
            Vector3 holoForward = Vector3.ProjectOnPlane(mainCameraTransform.forward, holoUp).normalized;

            if (holoForward.sqrMagnitude < 0.1f)
                holoForward = Vector3.ProjectOnPlane(mainCameraTransform.up, holoUp).normalized;

            hologramTransform.rotation = Quaternion.LookRotation(holoForward, holoUp);
        }
    }

    private void HandleGravityConfirmation()
    {
        if (isPreviewing && Input.GetKeyDown(KeyCode.Return))
        {
            CurrentGravityDir = targetGravityDir;
            isPreviewing = false;

            if (hologramTransform != null) hologramTransform.gameObject.SetActive(false);

            StartCoroutine(TransitionGravityRotation());
        }

        if (isPreviewing && Input.GetKeyDown(KeyCode.Escape))
        {
            isPreviewing = false;
            if (hologramTransform != null) hologramTransform.gameObject.SetActive(false);
        }
    }

    private IEnumerator TransitionGravityRotation()
    {
        isTransitioning = true;

        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null) pm.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Vector3 targetUp = -CurrentGravityDir;
        Vector3 targetForward = Vector3.ProjectOnPlane(mainCameraTransform.forward, targetUp).normalized;

        if (targetForward.sqrMagnitude < 0.1f)
        {
            targetForward = Vector3.ProjectOnPlane(mainCameraTransform.up, targetUp).normalized;
        }

        Quaternion targetRotation = Quaternion.LookRotation(targetForward, targetUp);
        float timeOut = 0f;

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

        transform.rotation = targetRotation;

        if (rb != null) rb.isKinematic = false;
        if (pm != null) pm.enabled = true;

        isTransitioning = false;
    }

    private Vector3 GetNearestAxis(Vector3 dir)
    {
        Vector3 nearest = Vector3.zero;
        float maxDot = -1f;

        Vector3[] axes = {
            Vector3.up, Vector3.down,
            Vector3.left, Vector3.right,
            Vector3.forward, Vector3.back
        };

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
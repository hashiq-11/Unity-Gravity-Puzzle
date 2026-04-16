using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(GravityController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    public Transform cameraTransform;

    [Header("Game Feel")]
    // These multipliers make the jump feel "snappier" and less floaty
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2.0f;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.2f;
    public float maxAirTime = 4.0f; // Kill-switch if player falls off the map

    private Rigidbody rb;
    private GravityController gravityController;
    private Vector3 moveInput;
    private bool isGrounded;
    private float currentAirTime = 0f;

    public Animator playerAnimator;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        gravityController = GetComponent<GravityController>();

        // We disable Unity's global gravity because we're applying our own 
        // manual force via the GravityController.
        rb.useGravity = false;
    }

    private void Update()
    {
        CheckGrounded();
        HandleInput();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        ApplyCustomGravity();
        MovePlayer();
    }

    private void HandleInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        moveInput = new Vector3(horizontal, 0f, vertical).normalized;

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            // Jump upwards relative to our current gravity-defying "up" direction
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void MovePlayer()
    {
        // Calculate movement relative to the camera's view and our local "up"
        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, transform.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, transform.up).normalized;
        Vector3 targetMoveDir = (camForward * moveInput.z + camRight * moveInput.x).normalized;

        float verticalSpeed = Vector3.Dot(rb.linearVelocity, transform.up);
        rb.linearVelocity = (targetMoveDir * moveSpeed) + (transform.up * verticalSpeed);

        // Rotate the character model to face the direction we are walking
        if (targetMoveDir.sqrMagnitude > 0.1f)
        {
            Quaternion lookRot = Quaternion.LookRotation(targetMoveDir, transform.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRot, 15f * Time.fixedDeltaTime));
        }
    }

    private void ApplyCustomGravity()
    {
        float verticalSpeed = Vector3.Dot(rb.linearVelocity, transform.up);
        Vector3 appliedGravity = gravityController.CurrentGravityDir * gravityController.gravityForce;

        // Apply "Mario-style" physics: fall faster when dropping, 
        // and fall faster if you let go of the jump button early.
        if (verticalSpeed < 0)
            appliedGravity *= fallMultiplier;
        else if (verticalSpeed > 0 && !Input.GetKey(KeyCode.Space))
            appliedGravity *= lowJumpMultiplier;

        rb.AddForce(appliedGravity, ForceMode.Acceleration);
    }

    private void CheckGrounded()
    {
        // Shoot a raycast down (relative to our local gravity) to see if we're standing on something
        Vector3 origin = transform.position + (transform.up * 0.1f);
        isGrounded = Physics.Raycast(origin, -transform.up, groundCheckDistance + 0.1f, groundLayer);

        if (!isGrounded)
        {
            currentAirTime += Time.deltaTime;
            // If the player falls for 4+ seconds, they've likely missed a platform and died
            if (currentAirTime >= maxAirTime)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerGameOver("You fell into the abyss!", false);
                }
                currentAirTime = 0f;
            }
        }
        else
        {
            currentAirTime = 0f;
        }
    }

    private void UpdateAnimations()
    {
        if (playerAnimator != null)
        {
            playerAnimator.SetFloat("Speed", moveInput.magnitude);
            playerAnimator.SetBool("isGrounded", isGrounded);
        }
    }
}
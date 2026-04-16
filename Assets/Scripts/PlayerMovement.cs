using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(GravityController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    public Transform cameraTransform;

    [Header("Game Feel")]
    // Multipliers to make the jump feel snappy and responsive, avoiding "floaty" physics
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2.0f;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.2f;
    public float maxAirTime = 4.0f; // Kill-switch threshold for falling off the map

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

        // Disable standard Unity gravity since the GravityController handles it dynamically
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
        float horizontal = 0f;
        float vertical = 0f;

        // Strictly enforce WASD for movement to keep Arrow Keys free for gravity manipulation
        if (Input.GetKey(KeyCode.W)) vertical += 1f;
        if (Input.GetKey(KeyCode.S)) vertical -= 1f;
        if (Input.GetKey(KeyCode.D)) horizontal += 1f;
        if (Input.GetKey(KeyCode.A)) horizontal -= 1f;

        moveInput = new Vector3(horizontal, 0f, vertical).normalized;

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            // Jump strictly along our current local "up" axis
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void MovePlayer()
    {
        // Align movement input with the camera's forward direction, mapped to the local ground plane
        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, transform.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, transform.up).normalized;
        Vector3 targetMoveDir = (camForward * moveInput.z + camRight * moveInput.x).normalized;

        float verticalSpeed = Vector3.Dot(rb.linearVelocity, transform.up);
        rb.linearVelocity = (targetMoveDir * moveSpeed) + (transform.up * verticalSpeed);

        // Smoothly rotate the character model to face the walking direction
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

        // "Mario-style" variable jump height: fall faster on the way down, 
        // or fall faster if the player taps the jump button instead of holding it
        if (verticalSpeed < 0)
            appliedGravity *= fallMultiplier;
        else if (verticalSpeed > 0 && !Input.GetKey(KeyCode.Space))
            appliedGravity *= lowJumpMultiplier;

        rb.AddForce(appliedGravity, ForceMode.Acceleration);
    }

    private void CheckGrounded()
    {
        // Cast a ray strictly downwards relative to the player's current orientation
        Vector3 origin = transform.position + (transform.up * 0.1f);
        isGrounded = Physics.Raycast(origin, -transform.up, groundCheckDistance + 0.1f, groundLayer);

        if (!isGrounded)
        {
            currentAirTime += Time.deltaTime;

            // Trigger game over if the player is in free-fall for too long (missed a platform)
            if (currentAirTime >= maxAirTime)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerGameOver("You fell into the abyss!", false);
                }
                currentAirTime = 0f; // Reset to prevent spamming the GameManager
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

using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Jump Buffer")]
    [SerializeField] private float jumpBufferTime = 0.15f;

    [Header("Coyote Time")]
    [SerializeField] private float coyoteTime = 0.15f;

    [Header("Jump Control")]
    [SerializeField] private float jumpCutMultiplier = 0.5f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 0.6f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerCombat playerCombat;

    private float moveInput;
    private bool isRunning;
    private bool isGrounded;

    private float jumpBufferCounter;
    private float coyoteTimeCounter;
    private bool jumpCutQueued;

    private bool isDashing;
    private float dashTimer;
    private float dashCooldownCounter;
    private float originalGravityScale;
    private int dashDirection;

    public bool IsDashing => isDashing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerCombat = GetComponent<PlayerCombat>();
    }

    private void Update()
    {
        HandleInput();
        HandleDashInput();

        CheckGround();
        UpdateCoyoteTime();

        UpdateAnimations();
        HandleFacing();
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            UpdateDash();
            return;
        }

        Move();
        HandleJump();
        HandleJumpCut();
    }

    private void HandleInput()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        isRunning =
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (rb.velocity.y > 0.01f)
            {
                jumpCutQueued = true;
            }
        }

        dashCooldownCounter -= Time.deltaTime;
    }

    private void Move()
    {
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        rb.velocity = new Vector2(
            moveInput * currentSpeed,
            rb.velocity.y
        );
    }

    private void HandleDashInput()
    {
        if (!Input.GetMouseButtonDown(1))
            return;

        if (isDashing || dashCooldownCounter > 0f)
            return;

        if (playerCombat != null && playerCombat.IsAttacking)
            return;

        StartDash();
    }

    private void StartDash()
    {
        isDashing = true;

        dashTimer = dashDuration;
        dashCooldownCounter = dashCooldown;

        originalGravityScale = rb.gravityScale;

        // Фиксируем направление рывка.
        if (Mathf.Abs(moveInput) > 0.01f)
        {
            dashDirection = moveInput > 0 ? 1 : -1;
        }
        else
        {
            dashDirection = spriteRenderer.flipX ? -1 : 1;
        }

        spriteRenderer.flipX = dashDirection < 0;

        // Во время Dash не действуют обычная гравитация
        // и вертикальная скорость.
        rb.gravityScale = 0f;
        rb.velocity = new Vector2(dashDirection * dashSpeed, 0f);

        animator.SetBool("IsDashing", true);
    }

    private void UpdateDash()
    {
        dashTimer -= Time.fixedDeltaTime;

        if (dashTimer <= 0f)
        {
            EndDash();
        }
    }

    public void CancelDash()
    {
        if (!isDashing)
            return;

        EndDash();
    }

    private void EndDash()
    {
        isDashing = false;

        rb.gravityScale = originalGravityScale;
        rb.velocity = Vector2.zero;

        animator.SetBool("IsDashing", false);
    }

    private void HandleJump()
    {
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            rb.velocity = new Vector2(
                rb.velocity.x,
                jumpForce
            );

            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
            isGrounded = false;
        }
    }

    private void HandleJumpCut()
    {
        if (!jumpCutQueued) return;

        if (rb.velocity.y > 0f)
        {
            rb.velocity = new Vector2(
                rb.velocity.x,
                rb.velocity.y * jumpCutMultiplier
            );
        }

        jumpCutQueued = false;
    }

    private void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    private void HandleFacing()
    {
        if (isDashing)
            return;

        if (playerCombat != null && playerCombat.IsAttacking)
            return;

        if (moveInput > 0)
            spriteRenderer.flipX = false;
        else if (moveInput < 0)
            spriteRenderer.flipX = true;
    }

    private void UpdateCoyoteTime()
    {
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private void UpdateAnimations()
    {
        float animationSpeed = 0f;

        if (Mathf.Abs(moveInput) > 0.01f)
        {
            animationSpeed = isRunning ? 1f : 0.5f;
        }

        animator.SetFloat("Speed", animationSpeed);
        animator.SetBool("IsGrounded", isGrounded);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundCheckRadius
        );
    }
}


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

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private float moveInput;
    private bool isRunning;
    private bool isGrounded;
    private float jumpBufferCounter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        HandleInput();
        CheckGround();
        UpdateAnimations();
        HandleFacing();
    }

    private void FixedUpdate()
    {
        Move();
        HandleJump();
    }

    private void HandleInput()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        isRunning =
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);

        // Запоминаем нажатие прыжка
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    private void Move()
    {
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        rb.velocity = new Vector2(
            moveInput * currentSpeed,
            rb.velocity.y
        );
    }

    private void HandleJump()
    {
        if (jumpBufferCounter > 0f && isGrounded)
        {
            rb.velocity = new Vector2(
                rb.velocity.x,
                jumpForce
            );

            jumpBufferCounter = 0f;
            isGrounded = false;
        }
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
        if (moveInput > 0)
            spriteRenderer.flipX = false;
        else if (moveInput < 0)
            spriteRenderer.flipX = true;
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

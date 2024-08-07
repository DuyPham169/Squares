using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float doubleJumpForce;
    private bool canDoubleJump;

    [Header("Buffer & Coyote Jump")]
    [SerializeField] private float bufferJumpWindow = .5f;
    private float bufferJumpActivated = -1;
    [SerializeField] private float coyoteJumpWindow = .5f;
    private float coyoteJumpActivated = -1;

    [Header("Wall Interactions")]
    [SerializeField] private float wallJumpDuration = .2f;
    [SerializeField] private Vector2 wallJumpForce;
    private bool isWallJumping;

    [Header("Knockback")]
    [SerializeField] private float knockbackDuration = 1;
    [SerializeField] private Vector2 knockbackPower;
    private bool isKnocked;

    [Header("Collision info")]
    [SerializeField] private float groundCheckDistance;
    [SerializeField] private float wallCheckDistance;
    [SerializeField] private LayerMask whatIsGround;
    private bool isGrounded;
    private bool isAirborne;
    private bool isWallDetected;

    private float xInput;
    private float yInput;

    private bool facingRight = true;
    private int facingDir = 1;

    // Start is called before the first frame update
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateAirborneStatus();

        if (isKnocked)
            return;

        HandleInput();
        HandleWallSlide();
        HandleMovement();
        HandleFlip();
        HandleCollision();
    }

    public void Knockback()
    {
        if (isKnocked)
            return;

        StartCoroutine(KnockbackRoutine());
        //anim.SetTrigger("knockback");
        rb.velocity = new Vector2(knockbackPower.x * -facingDir, knockbackPower.y);
    }

    private IEnumerator KnockbackRoutine()
    {
        isKnocked = true;
        yield return new WaitForSeconds(knockbackDuration);
        isKnocked = false;
    }

    private void UpdateAirborneStatus()
    {
        if (isGrounded && isAirborne)
        {
            HandleLanding();
        }
        if (!isGrounded && !isAirborne)
        {
            BecomeAirborne();
        }
    }

    private void BecomeAirborne()
    {
        isAirborne = true;

        if (rb.velocity.y <= 0)
        {
            ActivateCoyoteJump();
        }
    }

    private void HandleLanding()
    {
        isAirborne = false;
        canDoubleJump = true;

        AttemptBufferJump();
    }

    private void HandleWallSlide()
    {
        bool canWallSlide = isWallDetected && rb.velocity.y < 0;
        float yModifer = yInput < 0 ? 1 : .05f;

        if (canWallSlide == false)
            return;

        rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * yModifer);
    }

    private void HandleInput()
    {
        xInput = Input.GetAxisRaw("Horizontal");
        yInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(KeyCode.X))
        {
            JumpButton();
            RequestBufferJump();
        }
    }

    #region Buffer & Coyote Jump

    private void RequestBufferJump()
    {
        if (isAirborne)
            bufferJumpActivated = Time.time;
    }

    private void AttemptBufferJump()
    {
        if (Time.time < bufferJumpActivated + bufferJumpWindow)
        {
            bufferJumpActivated = 0; // resetting
            Jump();
        }
    }

    private void ActivateCoyoteJump() => coyoteJumpActivated = Time.time;

    private void CancelCoyoteJump() => coyoteJumpActivated = 0;

    #endregion

    private void JumpButton()
    {
        bool coyoteJumpAvailable = Time.time < coyoteJumpActivated + coyoteJumpWindow;
        if (isGrounded || coyoteJumpAvailable)
        {
            Jump();
        }
        else if (isWallDetected && !isGrounded)
        {
            WallJump();
        }
        else if (canDoubleJump)
        {
            DoubleJump();
        }

        CancelCoyoteJump();
    }

    private void Jump() => rb.velocity = new Vector2(rb.velocity.x, jumpForce);

    private void DoubleJump()
    {
        isWallJumping = false;
        canDoubleJump = false;
        rb.velocity = new Vector2(rb.velocity.x, doubleJumpForce);
    }

    private void WallJump()
    {
        canDoubleJump = true;
        rb.velocity = new Vector2(wallJumpForce.x * -facingDir, wallJumpForce.y);

        Flip();

        StopAllCoroutines();
        StartCoroutine(WallJumpRoutine());
    }

    private IEnumerator WallJumpRoutine()
    {
        isWallJumping = true;

        yield return new WaitForSeconds(wallJumpDuration);

        isWallJumping = false;
    }

    private void HandleCollision()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, whatIsGround);
        isWallDetected = Physics2D.Raycast(transform.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround);
    }
    private void HandleFlip()
    {
        if ((xInput < 0 && facingRight) || (xInput > 0 && !facingRight))
            Flip();
    }

    private void Flip()
    {
        transform.Rotate(0, 180, 0);
        facingRight = !facingRight;
        facingDir = facingDir * -1;
    }

    private void HandleMovement()
    {
        if (isWallJumping)
            return;

        rb.velocity = new Vector2(Input.GetAxisRaw("Horizontal") * moveSpeed, rb.velocity.y);
    }


    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x + (wallCheckDistance * facingDir), transform.position.y));
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x, transform.position.y - groundCheckDistance));
    }
}

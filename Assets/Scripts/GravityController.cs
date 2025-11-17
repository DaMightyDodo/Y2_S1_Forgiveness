using UnityEngine;
using UnityEngine.PlayerLoop;

public class GravityController : MonoBehaviour
{
    [Header("References")]
    public Rigidbody2D rb;
    public GravityStats stats;

    [Header("State (set by PlayerController)")]
    public bool isGrounded;
    public bool isJumping;
    public bool isJumpCut;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        ApplyGravity();
    }
    public void UpdateState(bool grounded, bool held, float velocityY)
    {
        isGrounded = grounded;
        isJumpCut = (!held && velocityY > 0);
        isJumping = (velocityY > 0 && !grounded);
    }

    private void ApplyGravity()
    {
        float velocityY = rb.linearVelocity.y;

        // 1. GROUND GRAVITY
        if (isGrounded)
        {
            rb.gravityScale = stats.defaultGravityScale;
            return;
        }

        // 2. End Jump Early
        if (isJumpCut && velocityY > 0)
        {
            rb.gravityScale = stats.defaultGravityScale * stats.jumpCutGravityMult;
            return;
        }

        // 3. HANG TIME (APEX)
        // small velocity near apex = low gravity
        if ((isJumping || velocityY > 0) && Mathf.Abs(velocityY) < stats.jumpHangTimeThreshold)
        {
            rb.gravityScale = stats.defaultGravityScale * stats.jumpHangGravityMult;
            return;
        }

        // 4. FALL GRAVITY
        if (velocityY < 0)
        {
            rb.gravityScale = stats.defaultGravityScale * stats.fallGravityMult;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(velocityY, -stats.maxFallSpeed));
            return;
        }

        // 5. DEFAULT GRAVITY
        rb.gravityScale = stats.defaultGravityScale;
    }
}
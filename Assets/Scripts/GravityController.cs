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
    public bool jumpHeld;
    public bool jumpCut;

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
        jumpCut = (!held && velocityY > 0);
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

        // 2. FAST FALL (holding down and falling)
        if (velocityY < 0 && stats.fastFallAllowed && Input.GetAxisRaw("Vertical") < 0)
        {
            rb.gravityScale = stats.defaultGravityScale * stats.fastFallGravityMult;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(velocityY, -stats.maxFastFallSpeed));
            return;
        }

        // 3. JUMP CUT (released early)
        if (jumpCut && velocityY > 0)
        {
            rb.gravityScale = stats.defaultGravityScale * stats.jumpCutGravityMult;
            return;
        }

        // 4. HANG TIME (APEX) — dino-style
        // EXACT SAME LOGIC AS DINO:
        // small velocity near apex = low gravity
        if ((isJumping || velocityY > 0) && Mathf.Abs(velocityY) < stats.jumpHangTimeThreshold)
        {
            rb.gravityScale = stats.defaultGravityScale * stats.jumpHangGravityMult;
            return;
        }

        // 5. FALL GRAVITY
        if (velocityY < 0)
        {
            rb.gravityScale = stats.defaultGravityScale * stats.fallGravityMult;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(velocityY, -stats.maxFallSpeed));
            return;
        }

        // 6. DEFAULT GRAVITY
        rb.gravityScale = stats.defaultGravityScale;
    }
}
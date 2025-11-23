using UnityEngine;
using UnityEngine.PlayerLoop;

public class GravityController : MonoBehaviour
{
    [SerializeField] private GravityStats _stats;        // ScriptableObject storing all gravity tuning values
    [SerializeField] private Rigidbody2D _rb;            // Reference to Rigidbody2D for velocity and gravityScale control
    private bool _isGrounded;                            // True when player is on ground
    private bool _isJumping;                             // True when player is rising (velocityY > 0)
    private bool _isJumpCut;                             // True when jump button released early during upward motion
    
    private void FixedUpdate()
    {
        ApplyGravity();                                  // All gravity logic handled in physics update
    }

    // Called by PlayerController to sync jump/ground state each frame (good decoupling)
    public void UpdateState(bool grounded, bool held, float velocityY)
    {
        _isGrounded = grounded;
        _isJumpCut = (!held && velocityY > 0);           // If jump released while moving up → apply jump cut gravity
        _isJumping = (velocityY > 0 && !grounded);       // Rising but not grounded = actively jumping
    }

    private void ApplyGravity()
    {
        float velocityY = _rb.linearVelocity.y;

        // 1. GROUND GRAVITY
        // Keep gravity normal on ground for consistent stickiness and fast state reset
        if (_isGrounded)
        {
            _rb.gravityScale = _stats.defaultGravityScale;
            return;
        }

        // 2. END JUMP EARLY
        // Higher gravity when player releases jump early to shorten jump height
        if (_isJumpCut && velocityY > 0)
        {
            _rb.gravityScale = _stats.defaultGravityScale * _stats.jumpCutGravityMult;
            return;
        }

        // 3. HANG TIME (APEX)
        // Reduce gravity near apex for smoother platformer jumps
        if ((_isJumping || velocityY > 0) && Mathf.Abs(velocityY) < _stats.jumpHangTimeThreshold)
        {
            _rb.gravityScale = _stats.defaultGravityScale * _stats.jumpHangGravityMult;
            return;
        }

        // 4. FALL GRAVITY
        // Increase gravity when falling, clamp max fall speed for better platformer control
        if (velocityY < 0)
        {
            _rb.gravityScale = _stats.defaultGravityScale * _stats.fallGravityMult;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, Mathf.Max(velocityY, -_stats.maxFallSpeed));
            return;
        }

        // 5. DEFAULT GRAVITY
        // Catch-all fallback (upward but not apex, not jump cut)
        _rb.gravityScale = _stats.defaultGravityScale;
    }
}

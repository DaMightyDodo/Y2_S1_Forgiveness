using UnityEngine;
using UnityEngine.PlayerLoop;

public class GravityController : MonoBehaviour
{
    [SerializeField] private GravityStats _stats;
    [SerializeField] private Rigidbody2D _rb;
    private bool _isGrounded;
    private bool _isJumping;
    private bool _isJumpCut;

    private void Reset()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        ApplyGravity();
    }
    public void UpdateState(bool grounded, bool held, float velocityY)
    {
        _isGrounded = grounded;
        _isJumpCut = (!held && velocityY > 0);
        _isJumping = (velocityY > 0 && !grounded);
    }

    private void ApplyGravity()
    {
        float velocityY = _rb.linearVelocity.y;

        // 1. GROUND GRAVITY
        if (_isGrounded)
        {
            _rb.gravityScale = _stats.defaultGravityScale;
            return;
        }

        // 2. End Jump Early
        if (_isJumpCut && velocityY > 0)
        {
            _rb.gravityScale = _stats.defaultGravityScale * _stats.jumpCutGravityMult;
            return;
        }

        // 3. HANG TIME (APEX)
        // near apex = low gravity for short duration
        if ((_isJumping || velocityY > 0) && Mathf.Abs(velocityY) < _stats.jumpHangTimeThreshold)
        {
            _rb.gravityScale = _stats.defaultGravityScale * _stats.jumpHangGravityMult;
            return;
        }

        // 4. FALL GRAVITY
        if (velocityY < 0)
        {
            _rb.gravityScale = _stats.defaultGravityScale * _stats.fallGravityMult;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, Mathf.Max(velocityY, -_stats.maxFallSpeed));
            return;
        }

        // 5. DEFAULT GRAVITY
        _rb.gravityScale = _stats.defaultGravityScale;
    }
}
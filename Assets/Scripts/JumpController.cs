using UnityEngine;

public class JumpController : MonoBehaviour
{
    private Rigidbody2D _rb;
    private PlayerStats _stats;

    private PlayerController _pc;

    private bool _isGrounded;
    private bool _isJumpHeld;

    private float _time;
    private float _timeJumpPressed = float.MinValue;
    private float _timeLeftGrounded;

    private void Awake()
    {
        _pc = GetComponent<PlayerController>();
        _rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        _pc.OnGroundedChanged += GroundedChanged;
        _pc.OnJumpHeldChanged += JumpHeldChanged;
        _pc.OnVelocityYChanged += VelocityYChanged;
    }

    private void OnDisable()
    {
        _pc.OnGroundedChanged -= GroundedChanged;
        _pc.OnJumpHeldChanged -= JumpHeldChanged;
        _pc.OnVelocityYChanged -= VelocityYChanged;
    }

    private void FixedUpdate()
    {
        _time += Time.fixedDeltaTime;
        TryJump();
    }

    private void GroundedChanged(bool isGrounded)
    {
        _isGrounded = isGrounded;

        if (!isGrounded)
            _timeLeftGrounded = _time;
    }

    private void JumpHeldChanged(bool isHeld)
    {
        _isJumpHeld = isHeld;

        if (isHeld)
            _timeJumpPressed = _time;
    }

    private void VelocityYChanged(float vy)
    {
        // optional apex logic later
    }

    private void TryJump()
    {
        bool canUseCoyote = !_isGrounded && _time < _timeLeftGrounded + _stats.coyoteTime;
        bool canUseBuffer = _isGrounded && _time < _timeJumpPressed + _stats.bufferTime;

        if (canUseCoyote || canUseBuffer)
        {
            // reset vertical velocity first for consistent jump
            var v = _rb.linearVelocity;
            v.y = 0;
            _rb.linearVelocity = v;

            // apply jump
            _rb.linearVelocityY = _stats.jumpForce;

            // consume buffered input
            _timeJumpPressed = float.MinValue;
        }
    }
}

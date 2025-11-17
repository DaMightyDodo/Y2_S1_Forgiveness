using System;
using Interfaces;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private BoxCollider2D _col;
    [SerializeField] private PlayerInputController _mPlayerInputController;
    [SerializeField] private PlayerStats _stats;
    [SerializeField] private JumpController _jump;
    [SerializeField] private GravityController _gravity;

    private IPlatformHandler _platformHandler;

    private float _horizontal;
    private Vector2 _frameVelocity;
    private bool _cachedQueryStartInColliders;
    private float _time;
    private bool _isCrouching;

    public event Action<bool> OnGroundedChanged;
    public event Action<bool> OnJumpHeldChanged;
    public event Action<float> OnVelocityYChanged;
    public event Action OnCharacterLanded;

    private bool _isGrounded;
    private bool _isDropping;
    private float _frameLeftGrounded = float.MinValue;

    private void Awake()
    {
        _platformHandler = GetComponent<IPlatformHandler>();
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<BoxCollider2D>();
        _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;
        _gravity = GetComponent<GravityController>();
    }

    private void OnEnable()
    {
        _mPlayerInputController.OnCharacterMove += Move;
        _mPlayerInputController.OnCharacterJump += Jump;
        _mPlayerInputController.OnCharacterCrouch += Crouch;
        _mPlayerInputController.OnCharacterDrop += Drop;
    }

    private void OnDisable()
    {
        _mPlayerInputController.OnCharacterMove -= Move;
        _mPlayerInputController.OnCharacterJump -= Jump;
        _mPlayerInputController.OnCharacterCrouch -= Crouch;
        _mPlayerInputController.OnCharacterDrop -= Drop;
    }

    private void Update()
    {
        if (!_isGrounded && _rb.linearVelocityY == 0) return;
        _time += Time.deltaTime;
    }

    private void FixedUpdate()
    {
        HandleMovement();
        HandleCrouch();
        CheckCollisions();
        BumpHeadCorrection();
        CatchJumpCorrection();

        OnVelocityYChanged?.Invoke(_rb.linearVelocity.y);

        _gravity.UpdateState(_isGrounded, _jump.IsHeld, _rb.linearVelocity.y);
        _platformHandler?.HandlePlatform();
    }

    private void Move(float inputValue)
    {
        _horizontal = inputValue;
    }

    private void Jump(bool isJumping)
    {
        OnJumpHeldChanged?.Invoke(isJumping);
        _jump.ReceiveInput(isJumping);
    }

    private void Crouch(bool isCrouching)
    {
        _isCrouching = isCrouching;
    }

    private void Drop(bool isDropping)
    {
        _isDropping = isDropping;
        _platformHandler?.SetDropping(isDropping);
    }

    private void HandleMovement()
    {
        if (_horizontal == 0)
        {
            var deceleration = _isGrounded ? _stats.groundDeceleration : _stats.airDeceleration;
            _frameVelocity.x = Mathf.MoveTowards(
                _frameVelocity.x, 0, deceleration * Time.fixedDeltaTime
            );
        }
        else
        {
            var targetSpeed = _horizontal * _stats.maxSpeed;
            if (_isGrounded && _isCrouching)
                targetSpeed *= _stats.crouchMultiplier;

            _frameVelocity.x = Mathf.MoveTowards(
                _frameVelocity.x, targetSpeed, _stats.acceleration * Time.fixedDeltaTime
            );
        }

        _rb.linearVelocity = new Vector2(_frameVelocity.x, _rb.linearVelocity.y);
    }

    private void HandleCrouch()
    {
        if (_isCrouching && _isGrounded && Mathf.Abs(_frameVelocity.x) > 0.01f)
        {
            var b = _col.bounds;
            LayerMask mask = ~_stats.playerLayer;

            var leftOuter = new Vector2(b.min.x, b.min.y);
            var leftInner = new Vector2(b.center.x - b.extents.x * _stats.innerRayOffset - _stats.rayOffsetX, b.min.y);
            var rightInner = new Vector2(b.center.x + b.extents.x * _stats.innerRayOffset + _stats.rayOffsetX, b.min.y);
            var rightOuter = new Vector2(b.max.x, b.min.y);

            bool leftOuterHit = Physics2D.Raycast(leftOuter, Vector2.down, _stats.ledgeCheckDistance, mask);
            bool leftInnerHit = Physics2D.Raycast(leftInner, Vector2.down, _stats.ledgeCheckDistance, mask);
            bool rightInnerHit = Physics2D.Raycast(rightInner, Vector2.down, _stats.ledgeCheckDistance, mask);
            bool rightOuterHit = Physics2D.Raycast(rightOuter, Vector2.down, _stats.ledgeCheckDistance, mask);

            bool leftEdge = leftOuterHit && !rightInnerHit;
            bool rightEdge = rightOuterHit && !leftInnerHit;

            if (leftEdge || rightEdge)
                _frameVelocity.x = 0f;
        }
    }

    private void CheckCollisions()
    {
        Physics2D.queriesStartInColliders = false;

        var hit = Physics2D.BoxCast(
            _col.bounds.center,
            _col.size,
            0,
            Vector2.down,
            _stats.grounderDistance,
            ~_stats.playerLayer
        );

        bool groundHit = hit.collider;

        if (!_isGrounded && groundHit)
        {
            _isGrounded = true;
            OnGroundedChanged?.Invoke(true);
            OnCharacterLanded?.Invoke();
        }
        else if (_isGrounded && !groundHit)
        {
            _isGrounded = false;
            _frameLeftGrounded = _time;
            OnGroundedChanged?.Invoke(false);
        }

        Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
    }

    private void BumpHeadCorrection()
    {
        if (_isGrounded || _rb.linearVelocity.y <= 0) return;

        var b = _col.bounds;
        var rayLength = _stats.ceilingCheckDistance;
        LayerMask mask = ~_stats.playerLayer;

        var leftOuter = new Vector2(b.min.x - _stats.outerRayOffset, b.max.y);
        var leftInner = new Vector2(b.center.x - b.extents.x * _stats.innerRayOffset, b.max.y);
        var rightInner = new Vector2(b.center.x + b.extents.x * _stats.innerRayOffset, b.max.y);
        var rightOuter = new Vector2(b.max.x + _stats.outerRayOffset, b.max.y);

        bool leftOuterHit = Physics2D.Raycast(leftOuter, Vector2.up, rayLength, mask);
        bool leftInnerHit = Physics2D.Raycast(leftInner, Vector2.up, rayLength, mask);
        bool rightInnerHit = Physics2D.Raycast(rightInner, Vector2.up, rayLength, mask);
        bool rightOuterHit = Physics2D.Raycast(rightOuter, Vector2.up, rayLength, mask);

        if (rightOuterHit && !rightInnerHit && !leftInnerHit && !leftOuterHit)
            _rb.position += Vector2.left * _stats.cornerCorrectionDistance;
        else if (leftOuterHit && !leftInnerHit && !rightInnerHit && !rightOuterHit)
            _rb.position += Vector2.right * _stats.cornerCorrectionDistance;
    }

    private void CatchJumpCorrection()
    {
        if (_rb.linearVelocity.y > 0 || Mathf.Abs(_horizontal) < 0.01f || _isDropping) return;

        var b = _col.bounds;
        LayerMask mask = ~_stats.playerLayer;
        var rayLength = _stats.edgeRayLength;
        var direction = Mathf.Sign(_frameVelocity.x);
        if (Mathf.Abs(direction) < 0.01f) return;

        Vector2 bottomOrigin = new Vector2(
            direction > 0 ? b.max.x : b.min.x,
            b.min.y + _stats.bottomRayOffset
        );

        Vector2 topOrigin = new Vector2(
            direction > 0 ? b.max.x : b.min.x,
            b.min.y + _stats.edgeInnerHeight + _stats.topRayOffset
        );

        bool bottomHit = Physics2D.Raycast(bottomOrigin, Vector2.right * direction, rayLength, mask);
        bool topHit = Physics2D.Raycast(topOrigin, Vector2.right * direction, rayLength, mask);

        if (bottomHit && !topHit)
        {
            _rb.position += Vector2.up * _stats.edgeCorrectionDistance;
            _isGrounded = true;
            OnGroundedChanged?.Invoke(true);
            _rb.linearVelocityY = 0;
        }
    }
}

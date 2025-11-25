using Interfaces;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Rigidbody2D _rb;                          // Rigidbody reference for movement physics
    [SerializeField] private BoxCollider2D _col;                       // Player collider for raycasts & size checks
    [SerializeField] private PlayerInputController mPlayerInputController; // Input events from new Input System
    [SerializeField] private PlayerStats _stats;                       // Scriptable Object containing movement stats
    [SerializeField] private GravityController gravity;                // Gravity handler that modifies vertical forces
    private IPlatformHandler _platformHandler;                         // Interface for one-way or fall-through platforms
    private float _horizontal;                                         // Horizontal input value
    private Vector2 _frameVelocity;                                    // X movement for this physics frame
    private bool _cachedQueryStartInColliders;                         // Backup to restore Physics2D setting
    private float _time;                                               // Global running time used for timing coyote/buffer
    private bool _reachedApex;                                         // True when upward velocity hits zero
    private bool _isCrouching;                                         // Input state for crouch
    private bool _coyoteAble;                                          // Whether player can still do a coyote jump
    private float _timeJumpWasPressed;                                 // When jump input occurred (for buffer)
    private bool _jumpHeld;                                            // Is jump button still held
    private Vector2 _tmpVelocity;                                      // Temporary working velocity
    private bool _isGrounded;                                          // Grounded check state
    private bool _isOnPlatform;                                        // If standing on one-way platform
    private bool _isDropping;                                          // If player is attempting to drop through
    private float _frameLeftGrounded = float.MinValue;                 // Timer for leaving ground (coyote)

    private void Awake()
    {
        _platformHandler = GetComponent<IPlatformHandler>();           // Get platform handler if supported
        _rb = GetComponent<Rigidbody2D>();                             // Assign RB from inspector or fetch
        _col = GetComponent<BoxCollider2D>();                          // Assign collider
        _coyoteAble = false;                                           // Coyote disabled until first grounded event
        _timeJumpWasPressed = float.MinValue;                          // No jump input buffered at start
        _cachedQueryStartInColliders = Physics2D.queriesStartInColliders; // Cache physics setting
        gravity = GetComponent<GravityController>();                   // Get gravity logic
    }

    // Handles per-frame logic like tracking time
    private void Update()
    {
        if (!_isGrounded && _rb.linearVelocityY == 0) return;          // Avoid updating time when frozen in air
        _time += Time.deltaTime;                                       // Internal timer for jump buffer/coyote
    }

    // Runs all main movement and physics updates
    private void FixedUpdate()
    {
        HandleMovement();                                              // Accel/decel for horizontal movement
        HandleJump();                                                  // Coyote, buffer, jump launching
        HandleCrouch();                                                // Ledge detection + forced stop when crouching
        CheckCollisions();                                             // Ground check
        BumpHeadCorrection();                                          // Fix bumping head on ceilings
        CatchJumpCorrection();                                         // Fix snagging when sliding along edges
        gravity.UpdateState(_isGrounded, _jumpHeld, _rb.linearVelocity.y); // Gravity reaction logic
        _platformHandler?.HandlePlatform();                            // One-way platform interactions
    }

    private void OnEnable()
    {
        // Subscribe to input callbacks
        mPlayerInputController.OnCharacterMove += Move;
        mPlayerInputController.OnCharacterJump += Jump;
        mPlayerInputController.OnCharacterCrouch += Crouch;
        mPlayerInputController.OnCharacterDrop += Drop;
    }

    private void OnDisable()
    {
        // Unsubscribe when disabled
        mPlayerInputController.OnCharacterMove -= Move;
        mPlayerInputController.OnCharacterJump -= Jump;
        mPlayerInputController.OnCharacterCrouch -= Crouch;
        mPlayerInputController.OnCharacterDrop -= Drop;
    }

    #region Gather Input

    // Stores horizontal movement input
    private void Move(float inputValue)
    {
        _horizontal = inputValue;
    }

    // Handles jump button press (buffer) and release (jump cut from GravityController)
    private void Jump(bool isJumping)
    {
        if (isJumping)
        {
            _timeJumpWasPressed = _time;                               // Stamp jump press time for buffer
            _jumpHeld = true;                                          // Jump held for gravity control
        }
        else
        {
            _jumpHeld = false;                                         // Released jump early
        }
    }

    // Input toggle for crouch
    private void Crouch(bool isCrouching)
    {
        _isCrouching = isCrouching;
    }

    // When pressing down to drop through one-way platforms
    private void Drop(bool isDropping)
    {
        _isDropping = isDropping;
        _platformHandler.SetDropping(isDropping);                      // Pass through interface
    }

    #endregion

    #region Horizontal

    // Handles walk acceleration/deceleration and crouch slowdown on ledges
    private void HandleMovement()
    {
        if (_horizontal == 0)
        {
            // Decelerate based on grounded/air state
            var deceleration = _isGrounded ? _stats.groundDeceleration : _stats.airDeceleration;
            _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, deceleration * Time.fixedDeltaTime);
        }
        else
        {
            var targetSpeed = _horizontal * _stats.maxSpeed;           // Base target speed

            if (!_isGrounded && _reachedApex)                          // Extra control at jump apex
                targetSpeed *= _stats.apexMultiplier;

            if (_isGrounded && _isCrouching)                           // Crouch slows movement
                targetSpeed *= _stats.crouchMultiplier;

            _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, targetSpeed, _stats.acceleration * Time.fixedDeltaTime);
        }

        _rb.linearVelocity = new Vector2(_frameVelocity.x, _rb.linearVelocity.y); // Apply horizontal only
    }

    #endregion

    #region Jumping

    // Performs buffered jump + coyote jump logic
    private void HandleJump()
    {
        // Coyote jump: falling but still within grace time after leaving ground
        var canUseCoyote =
            _coyoteAble &&
            !_isGrounded &&
            _time < _frameLeftGrounded + _stats.coyoteTime &&
            _time < _timeJumpWasPressed + _stats.bufferTime;

        // Buffered jump: jump pressed slightly before landing
        var canUseBuffer =
            _isGrounded &&
            _time < _timeJumpWasPressed + _stats.bufferTime;

        if (canUseCoyote || canUseBuffer)
        {
            _tmpVelocity = _rb.linearVelocity;                         // Reset downward velocity before jump
            _tmpVelocity.y = 0;
            _rb.linearVelocity = _tmpVelocity;

            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _stats.jumpForce); // Perform jump
            _coyoteAble = false;                                       // Consume coyote
            _timeJumpWasPressed = float.MinValue;                      // Consume buffer
        }
    }

    #endregion

    #region Crouching

    // Detects ledge crawl prevention while crouching + blocks sliding off edges
    private void HandleCrouch()
    {
        if (!_isCrouching || !_isGrounded || Mathf.Abs(_frameVelocity.x) < 0.01f) return;

        Bounds b = _col.bounds;                                        // Collider bounds for ray origins
        LayerMask mask = ~_stats.playerLayer;                          // Hit everything except player
        float dist = _stats.ledgeCheckDistance;

        // 4 downward rays across collider bottom for ledge detection
        Vector2 leftOuter  = new Vector2(b.min.x, b.min.y);
        Vector2 leftInner  = new Vector2(b.center.x - b.extents.x * _stats.innerRayOffset - _stats.rayOffsetX, b.min.y);
        Vector2 rightInner = new Vector2(b.center.x + b.extents.x * _stats.innerRayOffset + _stats.rayOffsetX, b.min.y);
        Vector2 rightOuter = new Vector2(b.max.x, b.min.y);

        bool leftOuterHit  = Physics2D.Raycast(leftOuter, Vector2.down, dist, mask);
        bool leftInnerHit  = Physics2D.Raycast(leftInner, Vector2.down, dist, mask);
        bool rightInnerHit = Physics2D.Raycast(rightInner, Vector2.down, dist, mask);
        bool rightOuterHit = Physics2D.Raycast(rightOuter, Vector2.down, dist, mask);

        // Debug visuals for accurate ledge debugging
        Debug.DrawRay(leftOuter,  Vector2.down * dist, leftOuterHit  ? Color.red : Color.green);
        Debug.DrawRay(leftInner,  Vector2.down * dist, leftInnerHit  ? Color.red : Color.green);
        Debug.DrawRay(rightInner, Vector2.down * dist, rightInnerHit ? Color.red : Color.green);
        Debug.DrawRay(rightOuter, Vector2.down * dist, rightOuterHit ? Color.red : Color.green);

        float dir = Mathf.Sign(_frameVelocity.x);                      // Current horizontal direction

        // Ledge on left: block rightward movement
        bool leftLedge = leftOuterHit && !leftInnerHit;
        if (leftLedge && dir > 0)
        {
            _frameVelocity.x = 0f;
            _rb.linearVelocityX = 0f;
        }

        // Ledge on right: block leftward movement
        bool rightLedge = rightOuterHit && !rightInnerHit;
        if (rightLedge && dir < 0)
        {
            _frameVelocity.x = 0f;
            _rb.linearVelocityX = 0f;
        }
    }

    #endregion

    #region Collision

    // Ground check using boxcast under player
    private void CheckCollisions()
    {
        Physics2D.queriesStartInColliders = false;                     // Required to avoid self-hit

        var hit = Physics2D.BoxCast(
            _col.bounds.center,
            _col.size,
            0,
            Vector2.down,
            _stats.grounderDistance,
            ~_stats.playerLayer
        );

        bool groundHit = hit.collider;

        // Landing event
        if (!_isGrounded && groundHit)
        {
            _isGrounded = true;
            _coyoteAble = true;                                        // Enable coyote jump
        }
        // Leaving ground event
        else if (_isGrounded && !groundHit)
        {
            _isGrounded = false;
            _frameLeftGrounded = _time;                                // Store time left ground
        }

        Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
    }

    #endregion

    #region Corner Correction

    // Prevents sticking when bumping head on ceiling corners
    private void BumpHeadCorrection()
    {
        if (_isGrounded || _rb.linearVelocity.y <= 0) return;          // Only when ascending

        var b = _col.bounds;
        var rayLength = _stats.ceilingCheckDistance;
        LayerMask mask = ~_stats.playerLayer;

        // Ceiling edge detection with 4 top raycasts
        var leftOuter = new Vector2(b.min.x - _stats.outerRayOffset, b.max.y);
        var leftInner = new Vector2(b.center.x - b.extents.x * _stats.innerRayOffset, b.max.y);
        var rightInner = new Vector2(b.center.x + b.extents.x * _stats.innerRayOffset, b.max.y);
        var rightOuter = new Vector2(b.max.x + _stats.outerRayOffset, b.max.y);

        bool leftOuterHit = Physics2D.Raycast(leftOuter, Vector2.up, rayLength, mask);
        bool leftInnerHit = Physics2D.Raycast(leftInner, Vector2.up, rayLength, mask);
        bool rightInnerHit = Physics2D.Raycast(rightInner, Vector2.up, rayLength, mask);
        bool rightOuterHit = Physics2D.Raycast(rightOuter, Vector2.up, rayLength, mask);

        // If only outer ray hits -> nudge sideways to slide around the ceiling corner
        if (rightOuterHit && !rightInnerHit && !leftInnerHit && !leftOuterHit)
            _rb.position += Vector2.left * _stats.cornerCorrectionDistance;
        else if (leftOuterHit && !leftInnerHit && !rightInnerHit && !rightOuterHit)
            _rb.position += Vector2.right * _stats.cornerCorrectionDistance;

        // Debugging rays
        Debug.DrawRay(leftOuter, Vector2.up * rayLength, leftOuterHit ? Color.red : Color.green);
        Debug.DrawRay(leftInner, Vector2.up * rayLength, leftInnerHit ? Color.red : Color.green);
        Debug.DrawRay(rightInner, Vector2.up * rayLength, rightInnerHit ? Color.red : Color.green);
        Debug.DrawRay(rightOuter, Vector2.up * rayLength, rightOuterHit ? Color.red : Color.green);
    }

    // Detect and correct ledge bumps while falling sideways
    private void CatchJumpCorrection()
    {
        if (_rb.linearVelocity.y > 0 || Mathf.Abs(_horizontal) < 0.01f || _isDropping) return;

        var b = _col.bounds;
        LayerMask mask = ~_stats.playerLayer;

        var rayLength = _stats.edgeRayLength;
        var direction = Mathf.Sign(_frameVelocity.x);                  // Movement direction left/right

        if (Mathf.Abs(direction) < 0.01f) return;                      // Not moving -> skip

        Vector2 bottomOrigin;
        Vector2 topOrigin;

        // Raycast origins for left/right movement
        if (direction > 0)
        {
            bottomOrigin = new Vector2(b.max.x, b.min.y + _stats.bottomRayOffset);
            topOrigin = new Vector2(b.max.x, b.min.y + _stats.edgeInnerHeight + _stats.topRayOffset);
        }
        else
        {
            bottomOrigin = new Vector2(b.min.x, b.min.y + _stats.bottomRayOffset);
            topOrigin = new Vector2(b.min.x, b.min.y + _stats.edgeInnerHeight + _stats.topRayOffset);
        }

        // Edge check: lower ray detects wall, upper ray detects if there's open air above
        bool bottomHit = Physics2D.Raycast(bottomOrigin, Vector2.right * direction, rayLength, mask);
        bool topHit = Physics2D.Raycast(topOrigin, Vector2.right * direction, rayLength, mask);

        // Visualize rays
        Debug.DrawRay(bottomOrigin, Vector2.right * (direction * rayLength), bottomHit ? Color.red : Color.green);
        Debug.DrawRay(topOrigin, Vector2.right * (direction * rayLength), topHit ? Color.red : Color.green);

        // If bottom hits but upper doesn’t -> sliding into a ledge while falling
        if (bottomHit && !topHit)
        {
            _rb.position += Vector2.up * _stats.edgeCorrectionDistance; // Nudge upward
            _isGrounded = true;                                         // Treat as landing
            _coyoteAble = true;                                         // Allow jump again
            _reachedApex = false;
            _rb.linearVelocityY = 0;                                    // Stop falling instantly
        }
    }

    #endregion
    
}

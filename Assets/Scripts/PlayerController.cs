    using System;
    using System.Collections;
    using Unity.VisualScripting;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.InputSystem.Interactions;

    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _rb;
        [SerializeField] private BoxCollider2D _col;
        [SerializeField] private PlayerInputController mPlayerInputController;
        [SerializeField] private PlayerStats _stats; //Scriptable Object
        [SerializeField] private PlatformCheck _platformCheck;
        private float _horizontal; //to get value from input action
        private Vector2 _frameVelocity;
        private bool _cachedQueryStartInColliders; //ignore player's _collider
        private float _time;
        private bool _reachedApex; //when player reached max jump height
        private bool _isCrouching;
        private bool _coyoteAble; //condition for jumping while falling
        private float _timeJumpWasPressed;
        private bool _endedJumpEarly;
        private bool _jumpHeld;
        private Vector2 _tmpVelocity;
        private bool _isGrounded; //ground check boolean
        private bool _isOnPlatform;
        private float _frameLeftGrounded = float.MinValue;
        // added for apex gravity handling
        private bool _apexLowGravityActive;
        private float _apexLowGravityTimer;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<BoxCollider2D>();
            _coyoteAble = false;
            _timeJumpWasPressed = float.MinValue;
            _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;
        }

        // Handles per-frame logic like tracking time
        private void Update()
        {
            if (!_isGrounded && _rb.linearVelocityY == 0) return;
            _time += Time.deltaTime;
        }

        // Runs all main movement and physics updates
        private void FixedUpdate()
        {
            HandleMovement();
            HandleJump();
            HandleCrouch();
            CheckCollisions();
            BumpHeadCorrection();
            CatchJumpCorrection();
            HandleGravity();
        }
        private void OnEnable()
        {
            mPlayerInputController.OnCharacterMove += Move;
            mPlayerInputController.OnCharacterJump += Jump;
            mPlayerInputController.OnCharacterCrouch += Crouch;
            mPlayerInputController.OnCharacterDrop += Drop;
        }

        private void OnDisable()
        {
            mPlayerInputController.OnCharacterMove -= Move;
            mPlayerInputController.OnCharacterJump -= Jump;
            mPlayerInputController.OnCharacterCrouch -= Crouch;
            mPlayerInputController.OnCharacterDrop -= Drop;
        }

        #region Gather Input
        // Receives horizontal movement input value
        private void Move(float inputValue)
        {
            _horizontal = inputValue;
        }
        // Handles jump button press and release
        private void Jump(bool isJumping)
        {
            if (isJumping)
            {
                _timeJumpWasPressed = _time;
                _jumpHeld = true;
            }
            else 
            {
                _jumpHeld = false;
            }
        }
        // Handles crouch button press and release
        private void Crouch(bool isCrouching)
        {
            _isCrouching = isCrouching;
        }

    private void Drop(bool isDropping)
    {
        _platformCheck.SetDropping(isDropping);
    }
        #endregion


        #region Horizontal


        // Handles horizontal acceleration, deceleration, and crouch ledge stop
        private void HandleMovement()
        {
            // Accelerate or decelerate horizontally
            if (_horizontal == 0)
            {
                float deceleration = _isGrounded ? _stats.groundDeceleration : _stats.airDeceleration;
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, deceleration * Time.fixedDeltaTime);
            }
            else
            {
                float targetSpeed = _horizontal * _stats.maxSpeed;
                // Apply apex multiplier while in air after reaching jump apex
                if (!_isGrounded && _reachedApex)
                {
                    targetSpeed *= _stats.apexMultiplier;
                }
                if (_isGrounded && _isCrouching)
                {
                    targetSpeed *= _stats.crouchMultiplier;
                }

                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, targetSpeed, _stats.acceleration * Time.fixedDeltaTime);

            }
            // Apply to rigidbody (keeping vertical movement)
            _rb.linearVelocity = new Vector2(_frameVelocity.x, _rb.linearVelocity.y);
        }
        #endregion
        
        #region Jumping


        // Handles jump logic, including coyote time and jump buffering
        private void HandleJump()
        {
            bool canUseCoyote = _coyoteAble && !_isGrounded && _time < _frameLeftGrounded + _stats.coyoteTime && _time < 
                _timeJumpWasPressed + _stats.bufferTime;
            bool canUseBuffer = _isGrounded && _time < _timeJumpWasPressed + _stats.bufferTime;

            if (canUseCoyote || canUseBuffer)
            {
                _tmpVelocity = _rb.linearVelocity;
                _tmpVelocity.y = 0;
                _rb.linearVelocity = _tmpVelocity;
                
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _stats.jumpForce); //jump!
                _coyoteAble = false;
                _timeJumpWasPressed = float.MinValue; // consume jump input
            }
        }
        #endregion

        private void HandleCrouch()
        {
                            // Prevent walking off ledge while crouching and grounded
                if (_isCrouching && _isGrounded && Mathf.Abs(_frameVelocity.x) > 0.01f)
                {
                    Bounds b = _col.bounds;
                    LayerMask mask = ~_stats.playerLayer;

                    // 4 downward rays
                    Vector2 leftOuter  = new Vector2(b.min.x, b.min.y);
                    Vector2 leftInner  = new Vector2(b.center.x - b.extents.x * _stats.innerRayOffset - _stats.rayOffsetX, b.min.y);
                    Vector2 rightInner = new Vector2(b.center.x + b.extents.x * _stats.innerRayOffset + _stats.rayOffsetX, b.min.y);
                    Vector2 rightOuter = new Vector2(b.max.x, b.min.y);

                    bool leftOuterHit  = Physics2D.Raycast(leftOuter, Vector2.down, _stats.ledgeCheckDistance, mask);
                    bool leftInnerHit  = Physics2D.Raycast(leftInner, Vector2.down, _stats.ledgeCheckDistance, mask);
                    bool rightInnerHit = Physics2D.Raycast(rightInner, Vector2.down, _stats.ledgeCheckDistance, mask);
                    bool rightOuterHit = Physics2D.Raycast(rightOuter, Vector2.down, _stats.ledgeCheckDistance, mask);

                    Debug.DrawRay(leftOuter, Vector2.down * _stats.ledgeCheckDistance, leftOuterHit ? Color.red : Color.green);
                    Debug.DrawRay(leftInner, Vector2.down * _stats.ledgeCheckDistance, leftInnerHit ? Color.red : Color.green);
                    Debug.DrawRay(rightInner, Vector2.down * _stats.ledgeCheckDistance, rightInnerHit ? Color.red : Color.green);
                    Debug.DrawRay(rightOuter, Vector2.down * _stats.ledgeCheckDistance, rightOuterHit ? Color.red : Color.green);

                    // Pairing: outer-left with inner-right, outer-right with inner-left
                    bool leftEdgeDrop  = leftOuterHit && !rightInnerHit;
                    bool rightEdgeDrop = rightOuterHit && !leftInnerHit;

                    if (leftEdgeDrop || rightEdgeDrop)
                    {
                        _frameVelocity.x = 0f;
                    }
                }
        }

        #region Collision
        // Checks ground contact using BoxCast and updates grounded state
    private void CheckCollisions()
    {
        Physics2D.queriesStartInColliders = false;

        // Perform a boxcast and store the hit info
        RaycastHit2D hit = Physics2D.BoxCast(_col.bounds.center, _col.size, 0, Vector2.down, _stats.grounderDistance, ~_stats.playerLayer);
        bool groundHit = hit.collider;

        // Landed on the Ground
        if (!_isGrounded && groundHit)
        {
            _isGrounded = true;
            _coyoteAble = true;
            _endedJumpEarly = false;
        }
        // is not grounded
        else if (_isGrounded && !groundHit)
        {
            _isGrounded = false;
            _frameLeftGrounded = _time;
        }



        Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
    }

        #endregion
        
        #region Corner Correction

        // Prevents head bump from sticking when hitting corners
        private void BumpHeadCorrection()
        {
            if (_isGrounded || _rb.linearVelocity.y <= 0) return;

            Bounds b = _col.bounds;
            float rayLength = _stats.ceilingCheckDistance;
            LayerMask mask = ~_stats.playerLayer;

            // 4 rays at the top of the _collider
            Vector2 leftOuter = new Vector2(b.min.x - _stats.outerRayOffset, b.max.y);
            Vector2 leftInner = new Vector2(b.center.x - b.extents.x * _stats.innerRayOffset, b.max.y);
            Vector2 rightInner = new Vector2(b.center.x + b.extents.x * _stats.innerRayOffset, b.max.y);
            Vector2 rightOuter = new Vector2(b.max.x + _stats.outerRayOffset, b.max.y);

            bool leftOuterHit  = Physics2D.Raycast(leftOuter, Vector2.up, rayLength, mask);
            bool leftInnerHit  = Physics2D.Raycast(leftInner, Vector2.up, rayLength, mask);
            bool rightInnerHit = Physics2D.Raycast(rightInner, Vector2.up, rayLength, mask);
            bool rightOuterHit = Physics2D.Raycast(rightOuter, Vector2.up, rayLength, mask);
            //TA NOTE 2 RAYCAST ONLY OR OTHER WAY PLZZ
            // Nudge conditions
            if (rightOuterHit && !rightInnerHit && !leftInnerHit && !leftOuterHit)
            {
                _rb.position += Vector2.left * _stats.cornerCorrectionDistance; // slide left around ledge
            }
            else if (leftOuterHit && !leftInnerHit && !rightInnerHit && !rightOuterHit)
            {
                _rb.position += Vector2.right * _stats.cornerCorrectionDistance; // slide right around ledge
            }

            // Debug visualize in Scene view
            Debug.DrawRay(leftOuter, Vector2.up * rayLength, leftOuterHit ? Color.red : Color.green);
            Debug.DrawRay(leftInner, Vector2.up * rayLength, leftInnerHit ? Color.red : Color.green);
            Debug.DrawRay(rightInner, Vector2.up * rayLength, rightInnerHit ? Color.red : Color.green);
            Debug.DrawRay(rightOuter, Vector2.up * rayLength, rightOuterHit ? Color.red : Color.green);
        }

        // Detects and corrects midair ledge bumps while falling
        private void CatchJumpCorrection()
        {
            if (_isGrounded || _rb.linearVelocity.y > 0 || Mathf.Abs(_horizontal) < 0.01f) return;

            Bounds b = _col.bounds;
            LayerMask mask = ~_stats.playerLayer;

            float rayLength = _stats.edgeRayLength;
            float direction = Mathf.Sign(_frameVelocity.x);

            // skip if not moving horizontally
            if (Mathf.Abs(direction) < 0.01f) return;

            Vector2 bottomOrigin;
            Vector2 topOrigin;
            
            //CAN BE 1 FUCNTION AND RECYCLE IT
            if (direction > 0)
            {
                // facing right
                bottomOrigin = new Vector2(b.max.x, b.min.y + _stats.bottomRayOffset);
                topOrigin = new Vector2(b.max.x, b.min.y + _stats.edgeInnerHeight + _stats.topRayOffset);
            }
            else
            {
                // facing left
                bottomOrigin = new Vector2(b.min.x, b.min.y + _stats.bottomRayOffset);
                topOrigin = new Vector2(b.min.x, b.min.y + _stats.edgeInnerHeight + _stats.topRayOffset);
            }
            //1 RAYCAST ONLY BRUV
            bool bottomHit = Physics2D.Raycast(bottomOrigin, Vector2.right * direction, rayLength, mask);
            bool topHit = Physics2D.Raycast(topOrigin, Vector2.right * direction, rayLength, mask);

            // Debug rays
            Debug.DrawRay(bottomOrigin, Vector2.right * (direction * rayLength), bottomHit ? Color.red : Color.green);
            Debug.DrawRay(topOrigin, Vector2.right * (direction * rayLength), topHit ? Color.red : Color.green);

            // Nudge upward if bottom ray hits but top ray does not (ledge bump)
            if (bottomHit && !topHit)
            {
                _rb.position += Vector2.up * _stats.edgeCorrectionDistance;
                _isGrounded = true;
                _coyoteAble = true;
                _reachedApex = false;
                _rb.linearVelocityY = 0;
            }
        }
        #endregion

        #region Gravity

        // Applies gravity, fall speed limits, and early jump release effects
        private void HandleGravity()
        {
            //WUT IS THIS ONLY CHANGE GRAVITY OF RIGIBODY 2D ONLY
            if (_isGrounded && _rb.linearVelocityY <= 0f)
            {
                _rb.linearVelocityY = _stats.groundingForce;
                _apexLowGravityActive = false;
            }
            else
            {
                float inAirGravity = _stats.fallAcceleration;

                // Detect early release inside physics, not input callback
                if (!_jumpHeld && !_isGrounded && _rb.linearVelocityY > 0)
                {
                    _endedJumpEarly = true;
                }

                // Apply stronger gravity if jump was released early
                if (_endedJumpEarly && _rb.linearVelocityY > 0)
                {   
                    inAirGravity *= _stats.jumpEndEarlyGravityModifier;
                }

                // handle temporary low gravity after apex
                if (_apexLowGravityActive)
                {
                    inAirGravity *= _stats.apexLowGravityMultiplier;
                    _apexLowGravityTimer -= Time.fixedDeltaTime;
                    if (_apexLowGravityTimer <= 0)
                    {
                        _apexLowGravityActive = false;
                    }
                }

                _rb.linearVelocityY = Mathf.MoveTowards(
                    _rb.linearVelocityY,
                    -_stats.maxFallSpeed,
                    inAirGravity * Time.fixedDeltaTime
                );
            }

            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _rb.linearVelocityY);

            // Detect apex: when upward velocity changes to downward
            if (!_isGrounded && !_reachedApex && _jumpHeld && _rb.linearVelocityY <= 0)
            {
                _reachedApex = true;

            }
            else if (_isGrounded)
            {
                _reachedApex = false; // reset when touching ground
            }
            if (_reachedApex)
            {
                _apexLowGravityActive = true;
            }
        }
        #endregion
    }

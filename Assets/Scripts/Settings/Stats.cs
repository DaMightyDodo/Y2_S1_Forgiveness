using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "Scriptable Objects/PlayerStats")]
public class PlayerStats : ScriptableObject
{
    [Header("Layers & Collision")]
    public LayerMask playerLayer;

    [Tooltip("How far below the player the cast checks for ground contact.")]
    public float grounderDistance = 0.05f;

    [Header("Jump Settings")]
    [Tooltip("Initial upward force applied when jumping.")]
    public float jumpForce = 5f;

    [Tooltip("How long after leaving the ground the player can still jump.")]
    public float coyoteTime = 0.15f;

    [Tooltip("How long before landing a jump input can still register.")]
    public float bufferTime = 0.2f;
    
    [Header("Horizontal Movement")]
    [Tooltip("Maximum horizontal movement speed.")]
    public float maxSpeed = 14f;

    [Tooltip("Horizontal acceleration rate.")]
    public float acceleration = 120f;

    [Tooltip("Horizontal deceleration rate when grounded.")]
    public float groundDeceleration = 60f;

    [Tooltip("Horizontal deceleration rate when in the air.")]
    public float airDeceleration = 30f;

    [Tooltip("Multiplier applied to airspeed after reaching jump apex to help reversing direction.")]
    public float apexMultiplier = 2f;

    [Header("Crouch Settings")]
    [Tooltip("Speed multiplier when crouching.")]
    public float crouchMultiplier = 0.4f;
    [Tooltip("How far below the player to check for ledge when crouching.")]
    public float ledgeCheckDistance = 0.2f;
    [Tooltip("Global horizontal offset for crouch ledge detection rays.")]
    public float rayOffsetX = 0.2f;
    [Header("Bumped Head Correction")]
    [Tooltip("Distance above the player checked for ceiling collisions.")]
    public float ceilingCheckDistance = 0.5f;

    [Tooltip("Horizontal distance to nudge the player away from corners when bumping the head.")]
    public float cornerCorrectionDistance = 0.2f;

    [Tooltip("Offset ratio for the inner ray in corner correction.")]
    public float innerRayOffset = 0.7f;

    [Tooltip("Offset ratio for the outer ray in corner correction.")]
    public float outerRayOffset = 0.2f;

    [Header("Catch Jump Correction")]
    [Tooltip("How far sideways the rays check for nearby edges.")]
    public float edgeRayLength = 0.2f;

    [Tooltip("Vertical offset for the upper ray in edge detection.")]
    public float edgeInnerHeight = 0.25f;

    [Tooltip("How much to lift the player up when caught by edge forgiveness.")]
    public float edgeCorrectionDistance = 0.1f;

    [Tooltip("Inward horizontal offset for the upper ray.")]
    public float topRayOffset = 0.02f;

    [Tooltip("Outward horizontal offset for the lower ray.")]
    public float bottomRayOffset = 0.02f;
    

}

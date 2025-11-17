using UnityEngine;

[CreateAssetMenu(fileName = "JumpStats", menuName = "Scriptable Objects/JumpStats")]
public class JumpStats : ScriptableObject
{
    [Header("Jump Settings")]
    [Tooltip("Initial upward force applied when jumping.")]
    public float jumpForce = 5f;

    [Tooltip("Coyote time duration after leaving ground where jump is still allowed.")]
    public float coyoteTime = 0.15f;

    [Tooltip("Jump buffer duration before landing.")]
    public float bufferTime = 0.2f;
}
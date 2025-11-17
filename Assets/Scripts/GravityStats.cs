using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/GravityStats")]
public class GravityStats : ScriptableObject
{
    public float defaultGravityScale = 1f;

    [Header("Fall")]
    public float fallGravityMult = 1.5f;
    public float maxFallSpeed = 16f;
    
    [Header("Jump Cut")]
    public float jumpCutGravityMult = 2f;

    [Header("Hang Time (Apex)")]
    public float jumpHangGravityMult = 0.5f;
    public float jumpHangTimeThreshold = 0.1f;
}
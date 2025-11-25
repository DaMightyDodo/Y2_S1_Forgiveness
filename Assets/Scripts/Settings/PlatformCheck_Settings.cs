using UnityEngine;

[CreateAssetMenu(fileName = "PlatformCheckSettings", menuName = "Scriptable Objects/PlatformCheckSettings")]
public class PlatformCheckSettings : ScriptableObject
{
    [Tooltip("Vertical offset to start the BoxCast slightly below the player's collider")]
    public float rayOffset = 0.05f;

    [Tooltip("Distance to cast the BoxCast downward")]
    public float castDistance = 0.5f;

    [Tooltip("Thickness of the BoxCast used to detect platforms below the player")]
    public float boxThickness = 0.05f;
}
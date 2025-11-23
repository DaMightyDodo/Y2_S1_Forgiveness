using UnityEngine;

[CreateAssetMenu(fileName = "CameraSettings", menuName = "Scriptable Objects/CameraSettings")]
public class CameraSettings : ScriptableObject
{
    public float interpVelocity;               // The calculated velocity at which the camera approaches the target.
    public float followSpeedMultiplier;        // A multiplier that controls how fast the camera catches up.
    public float smoothMultiplier;             //
    public Vector3 offset;                     // A positional offset added to the camera’s final position.
}

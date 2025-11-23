using UnityEngine; 
using System.Collections;    

public class FollowCamera : MonoBehaviour {     //makes camera follow a _target object (Player)

    [SerializeField] private CameraSettings _stats;
    [SerializeField] private GameObject _target;                  // The object the camera should follow.
    private Vector3 _targetPos;                         // Internal variable storing the next desired camera position.

    private void Start() 
    {
        _targetPos = transform.position; // Initialize _targetPos to the starting camera position.
    }
    private void FixedUpdate() 
    {
        if (_target) // Only update if the camera has a _target assigned.
        {
            Vector3 posNoZ = transform.position;   // Start with the camera's current position
            posNoZ.z = _target.transform.position.z; //match the _target's Z so direction stays planar.

            Vector3 targetDirection = (_target.transform.position - posNoZ);
            // Direction vector pointing from the camera toward the _target (ignoring original Z difference).

            _stats.interpVelocity = targetDirection.magnitude * _stats.followSpeedMultiplier;
            // The catch-up speed scales with distance. Farther _target = faster follow.

            _targetPos = transform.position + (targetDirection.normalized * (_stats.interpVelocity * Time.deltaTime));
            // Compute an intermediate position by moving along the direction at the calculated speed.

            transform.position = Vector3.Lerp(transform.position, _targetPos + _stats.offset, _stats.smoothMultiplier);
            // Smoothly interpolate from current position toward the desired position plus the offset.
        }
    }
}

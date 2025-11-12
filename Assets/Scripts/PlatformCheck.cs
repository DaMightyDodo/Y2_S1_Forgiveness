using UnityEngine;

public class PlatformCheck : MonoBehaviour
{
    [SerializeField] private BoxCollider2D playerCollider;
    [SerializeField] private LayerMask platformLayer;
    private bool _isOnPlatform;
    private bool _isDropping;
    private Collider2D _currentPlatform;

    public bool IsOnPlatform => _isOnPlatform;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if we landed on a platform layer
        if (collision.gameObject.layer == LayerMask.NameToLayer("Platform"))
        {
            _isOnPlatform = true;
            _currentPlatform = collision.collider;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // When we leave a platform
        if (collision.collider == _currentPlatform)
        {
            _isOnPlatform = false;
            _currentPlatform = null;
            StopIgnoringPlatform();
        }
    }

    public void SetDropping(bool isDropping)
    {
        // Only drop if we're currently on a platform
        if (isDropping && _isOnPlatform && !_isDropping && _currentPlatform != null)
        {
            _isDropping = true;
            Physics2D.IgnoreCollision(playerCollider, _currentPlatform, true);
        }
    }

    private void StopIgnoringPlatform()
    {
        if (_currentPlatform != null)
        {
            Physics2D.IgnoreCollision(playerCollider, _currentPlatform, false);
            Debug.Log("Ignoring platform");
        }
        _isDropping = false;
    }
}
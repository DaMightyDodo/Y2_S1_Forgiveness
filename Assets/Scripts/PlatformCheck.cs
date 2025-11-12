using UnityEngine;

public class PlatformCheck : MonoBehaviour
{
    [SerializeField] private BoxCollider2D playerCollider;
    [SerializeField] private LayerMask platformLayer;

    private bool _isDropping;
    private bool _isOnPlatform;

    public bool IsOnPlatform => _isOnPlatform;

    public void SetDropping(bool isDropping)
    {
        _isDropping = isDropping;

        if (_isDropping && _isOnPlatform)
        {
            if (playerCollider.enabled)
                playerCollider.enabled = false; // drop through
        }
        else
        {
            if (!playerCollider.enabled)
                playerCollider.enabled = true; // re-enable when input released
        }
    }

    // Call this in FixedUpdate or via PlayerController
    public void HandlePlatformDrop()
    {
        // Nothing else needed for now; collider is toggled via SetDropping
    }

    private void OnCollisionEnter2D(Collision2D _collision)
    {
        // Landed on Platform (specific layer check)
        if (_collision.gameObject.layer == LayerMask.NameToLayer("Platform"))
        {
            _isOnPlatform = true;
            Debug.Log("Landed on Platform");
        }
    }

    private void OnCollisionExit2D(Collision2D _collision)
    {
        if (_collision.gameObject.layer == LayerMask.NameToLayer("Platform"))
        {
            _isOnPlatform = false;
            Debug.Log("Left Platform");
        }
    }

}
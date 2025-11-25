using UnityEngine;
using Interfaces;

public class PlatformCheck : MonoBehaviour, IPlatformHandler
{
    [SerializeField] private BoxCollider2D playerCollider; // player collider used for enabling/disabling collision with platforms
    [SerializeField] private LayerMask platformLayer;      // layers that count as semi-solid platforms
    [SerializeField] private PlatformCheckSettings settings; // config for raycast offsets and box thickness

    private bool _isOnPlatform;          // true while standing on a platform via collision
    private Collider2D _currentPlatform; // reference to the platform currently stood on
    private bool _isRayCasting;          // true when waiting to detect ground after dropping

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // check if collided object is part of platform layer
        if (((1 << collision.gameObject.layer) & platformLayer) > 0)
        {
            _isOnPlatform = true;
            _currentPlatform = collision.collider; // store platform to later ignore/reenable collision
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // when leaving the currently tracked platform, begin raycast recovery
        if (collision.collider == _currentPlatform)
        {
            _isOnPlatform = false;
            _isRayCasting = true; // start checking for new ground
        }
    }

    public void SetDropping(bool isDropping)
    {
        // triggered by controller when player wants to drop through
        if (isDropping && _isOnPlatform && _currentPlatform != null)
        {
            Physics2D.IgnoreCollision(playerCollider, _currentPlatform, true); // disable collision to fall through
            _isRayCasting = true; // start raycast process to reenable when safe
        }
    }

    public void HandlePlatform()
    {
        if (_isRayCasting)
        {
            // cast a thin box slightly below the player's feet to detect ground again
            Vector2 boxOrigin = new Vector2(playerCollider.bounds.center.x, playerCollider.bounds.min.y - settings.rayOffset);
            Vector2 boxSize = new Vector2(playerCollider.bounds.size.x, settings.boxThickness); // very thin vertical slice
            float distance = settings.castDistance;

            RaycastHit2D hit = Physics2D.BoxCast(boxOrigin, boxSize, 0f, Vector2.down, distance);

            // debug visualization
            Debug.DrawRay(boxOrigin, Vector2.down * distance, hit.collider ? Color.green : Color.red);

            // once ground is detected again, reenable collision with the platform
            if (hit.collider)
            {
                Physics2D.IgnoreCollision(playerCollider, _currentPlatform, false);
                _isRayCasting = false; // stop raycasting until next drop
            }
        }
    }
}
using UnityEngine;
using Interfaces;
public class PlatformCheck : MonoBehaviour, IPlatformHandler
{
    [SerializeField] private BoxCollider2D playerCollider;
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private PlatformCheckSettings settings;
    private bool _isOnPlatform;
    private Collider2D _currentPlatform;
    public bool _isRayCasting;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & platformLayer) > 0)
        {
            _isOnPlatform = true;
            _currentPlatform = collision.collider;
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider == _currentPlatform)
        {
            _isOnPlatform = false;
            _isRayCasting = true;
        }
    }
    public void SetDropping(bool isDropping)
    {
        if (isDropping && _isOnPlatform && _currentPlatform != null)
        {
            Physics2D.IgnoreCollision(playerCollider, _currentPlatform, true);
            _isRayCasting = true;
        }
    }
    public void HandlePlatform()
    {
        if (_isRayCasting)
        {
            // BoxCast slightly below the player
            Vector2 boxOrigin = new Vector2(playerCollider.bounds.center.x, playerCollider.bounds.min.y - settings.rayOffset);
            Vector2 boxSize = new Vector2(playerCollider.bounds.size.x, settings.boxThickness); // thin vertical box
            float distance = settings.castDistance;

            RaycastHit2D hit = Physics2D.BoxCast(boxOrigin, boxSize, 0f, Vector2.down, distance);

            // Debug
            Debug.DrawRay(boxOrigin, Vector2.down * distance, hit.collider ? Color.green : Color.red);

            // Stop ignoring collision if anything is hit
            if (hit.collider)
            {
                Physics2D.IgnoreCollision(playerCollider, _currentPlatform, false);
                _isRayCasting = false;
            }
        }
    }
}
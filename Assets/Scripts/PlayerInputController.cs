using UnityEngine;
using UnityEngine.InputSystem;
using System;

// Broadcasts input events to PlayerController
public class PlayerInputController : MonoBehaviour
{
    public event Action<float> OnCharacterMove;   // Sends horizontal movement value
    public event Action<bool> OnCharacterJump;    // Sends jump press/release
    public event Action<bool> OnCharacterCrouch;  // Sends crouch press/release
    public event Action<bool> OnCharacterDrop;    // Sends drop-through-platform press/release

    public void OnMove(InputAction.CallbackContext context)
    {
        // Read x-axis input from movement vector
        OnCharacterMove?.Invoke(context.ReadValue<Vector2>().x);
    }
    
    public void OnJump(InputAction.CallbackContext context)
    {
        // Jump pressed
        if(context.started)
            OnCharacterJump?.Invoke(true);
        
        // Jump released
        if(context.canceled)
            OnCharacterJump?.Invoke(false);
    }
    
    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            // Crouch pressed
            OnCharacterCrouch?.Invoke(true);
        }
        else if (context.canceled)
        {
            // Crouch released
            OnCharacterCrouch?.Invoke(false);
        }
    }

    public void OnDrop(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            // Drop-through-platform pressed
            OnCharacterDrop?.Invoke(true);
            Debug.Log("pressed down");
        }
        else if (context.canceled)
        {
            // Drop-through-platform released
            OnCharacterDrop?.Invoke(false);
        }
    }
}
using UnityEngine;
using UnityEngine.InputSystem;
using System;
//broadcast to PlayerController with Event Action
public class PlayerInputController : MonoBehaviour
{
    public event Action<float> OnCharacterMove;
    public event Action<bool> OnCharacterJump;
    public event Action<bool> OnCharacterCrouch;
    public event Action<bool> OnCharacterDrop;
    public void OnMove(InputAction.CallbackContext context)
    {
        OnCharacterMove?.Invoke(context.ReadValue<Vector2>().x);
    }
    
    public void OnJump(InputAction.CallbackContext context)
    {
        if(context.started)
            OnCharacterJump?.Invoke(true);
        if(context.canceled)
            OnCharacterJump?.Invoke(false);
    }
    
    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            OnCharacterCrouch?.Invoke(true);
        }
        else if (context.canceled)
        {
            OnCharacterCrouch?.Invoke(false);
        }
    }

    public void OnDrop(InputAction.CallbackContext context)
    {
        if (context.started)
        { 
            OnCharacterDrop?.Invoke(true);
            Debug.Log("pressed down");
        }
        else if (context.canceled)
        { 
            OnCharacterDrop?.Invoke(false);
        }
    }
}

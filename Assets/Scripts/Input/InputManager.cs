using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class InputManager : SingletonUtil<InputManager>, InputSystem_Actions.IPlayerActions
{
    private InputSystem_Actions input;

    public Action<Vector2> OnMoveEvent;
    public Action<bool> OnJumpEvent;
    public Action<Vector2> OnLookEvent;

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif

    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        //throw new System.NotImplementedException();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        //throw new System.NotImplementedException();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        //throw new System.NotImplementedException();
        OnJumpEvent?.Invoke(context.ReadValueAsButton());
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        //throw new System.NotImplementedException();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        //throw new System.NotImplementedException();
        if (context.started || context.performed)
            OnMoveEvent?.Invoke(context.ReadValue<Vector2>());
        else if (context.canceled)
            OnMoveEvent?.Invoke(Vector2.zero);
    }

    public void OnNext(InputAction.CallbackContext context)
    {
        //throw new System.NotImplementedException();
    }

    public void OnPrevious(InputAction.CallbackContext context)
    {
        //throw new System.NotImplementedException();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        //throw new System.NotImplementedException();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Awake()
    {
        base.Awake();
        input = new InputSystem_Actions();
        input.Disable();
        input.Player.AddCallbacks(this);
    }

    private void OnDestroy()
    {
        input.Dispose();
    }

    private void OnEnable()
    {
        input.Enable();
    }

    private void OnDisable()
    {
        input.Disable();
    }
}

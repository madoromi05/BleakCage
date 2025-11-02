using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelectInputReader : MonoBehaviour, InputControls.ISelectActionActions
{
    public event Action UpStatusEvent;
    public event Action DownStatusEvent;
    public event Action ConfirmEvent;

    private InputControls controls;

    private void OnEnable()
    {
        if (controls == null)
        {
            controls = new InputControls();
            controls.SelectAction.SetCallbacks(this);
        }
        controls.SelectAction.Enable();
        Debug.Log("SelectInputReader enabled and controls set up.");
    }

    private void OnDisable()
    {
        controls?.SelectAction.Disable();
    }

    public void OnUpStatus(InputAction.CallbackContext context)
    {
        Debug.Log("OnUpStatus called.");
        if (!context.performed) { return; }
        UpStatusEvent?.Invoke();
        Debug.Log("UpStatusEvent invoked.");
    }

    public void OnDownStatus(InputAction.CallbackContext context)
    {
        if (!context.performed) { return; }
        DownStatusEvent?.Invoke();
    }

    public void OnConfirm(InputAction.CallbackContext context)
    {
        if (!context.performed) { return; }
        ConfirmEvent?.Invoke();
    }
}

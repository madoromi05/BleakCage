using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelectInputReader : MonoBehaviour, InputControls.ISelectActionActions
{
    public event Action UpStatusEvent;
    public event Action DownStatusEvent;
    public event Action ConfirmEvent;

    private InputControls controls;
    private void Awake()
    {
        if (controls == null)
        {
            controls = new InputControls();
            controls.SelectAction.SetCallbacks(this);
        }
    }

    private void OnEnable()
    {
        EnableActionMap();
    }

    private void OnDisable()
    {
        DisableActionMap();
    }

    public void EnableActionMap()
    {
        if (controls == null)
        {
            controls = new InputControls();
            controls.SelectAction.SetCallbacks(this);
        }
        controls.SelectAction.Enable();
        Debug.Log("SelectAction Map Manually ENABLED.");
    }
    public void DisableActionMap()
    {
        controls?.SelectAction.Disable();
        Debug.Log("SelectAction Map Manually DISABLED.");
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
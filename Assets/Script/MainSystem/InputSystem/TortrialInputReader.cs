using System;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;

public class TortrialInputReader : MonoBehaviour, InputControls.ITutorialActionActions
{
    public event Action OnProceed;
    private InputControls controls;

    private void Awake()
    {
        controls = new InputControls();
        controls.TutorialAction.SetCallbacks(this);
    }

    private void OnEnable()
    {
        controls.TutorialAction.Enable();
    }

    private void OnDisable()
    {
        controls.TutorialAction.Disable();
    }

    void InputControls.ITutorialActionActions.OnProceed(InputAction.CallbackContext context)
    {
        if (!context.performed) { return; }
        OnProceed?.Invoke();
    }
}

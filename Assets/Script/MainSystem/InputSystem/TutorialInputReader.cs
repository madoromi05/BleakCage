using System;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialInputReader : MonoBehaviour, InputControls.ITutorialActionActions
{
    public event Action OnProceed;
    private InputControls controls;


    private void OnEnable()
    {
        if (controls == null)
        {
            controls = new InputControls();
            controls.TutorialAction.SetCallbacks(this);
        }
        controls.TutorialAction.Enable();
    }

    private void OnDisable()
    {
        controls?.TutorialAction.Disable();
    }

    void InputControls.ITutorialActionActions.OnProceed(InputAction.CallbackContext context)
    {
        if (!context.performed) { return; }
        OnProceed?.Invoke();
    }
}

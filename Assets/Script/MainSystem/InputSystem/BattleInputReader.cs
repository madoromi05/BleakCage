using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class BattleInputReader : MonoBehaviour, InputControls.IBattleActionActions
{
    public event Action DisCardEvent;
    public event Action<int> CardSelectEvent;

    private InputControls controls;

    private void Awake()
    {
        controls = new InputControls();
        controls.BattleAction.SetCallbacks(this);
    }

    private void OnEnable()
    {
        controls?.BattleAction.Disable();
        controls.BattleAction.Enable();
    }

    private void OnDisable()
    {
        controls?.BattleAction.Disable();
        controls.BattleAction.Disable();
    }

    /// <summary>
    /// Input action methods
    /// </summary>
    /// <param name="context"></param>
    public void OnDisCard(InputAction.CallbackContext context)
    {
        if (!context.performed) { return; }
        DisCardEvent?.Invoke();
    }

    public void OnCardSelectOne(InputAction.CallbackContext context)
    {
        if (!context.performed) { return; }
        CardSelectEvent?.Invoke(0);
    }

    public void OnCardSelectTwo(InputAction.CallbackContext context)
    {
        if (!context.performed) { return; }
        CardSelectEvent?.Invoke(1);
    }

    public void OnCardSelectTree(InputAction.CallbackContext context)
    {
        if (!context.performed) { return; }
        CardSelectEvent?.Invoke(2);
    }
}

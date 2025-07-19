using System;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// <summary>
/// 入力を管理するクラス
/// </summary>

public class InputReader : MonoBehaviour, Controls.IBattaleActionActions
{
    public event Action DisCardEvent;

    private Controls controls;

    void Start()
    {
        controls = new Controls();
        controls.BattaleAction.SetCallbacks(this);
        controls.BattaleAction.Enable();
    }

    public void OnDisCard(InputAction.CallbackContext context)
    {
        //入力が「実行された瞬間」でなければ終了
        if (!context.performed) { return; }
        DisCardEvent?.Invoke();
    }
}

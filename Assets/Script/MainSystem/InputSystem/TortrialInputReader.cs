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
        Debug.Log("TortrialInputReader: 有効化され、入力アクションが有効になりました。");
    }

    private void OnDisable()
    {
        controls.TutorialAction.Disable();
        Debug.Log("TortrialInputReader: 無効化され、入力アクションが無効になりました。");
    }

    private void OnDestroy()
    {
        Debug.LogError("TortrialInputReader: OnDestroyが呼び出されました。このGameObjectは破棄されます。");
    }

    void InputControls.ITutorialActionActions.OnProceed(InputAction.CallbackContext context)
    {
        if (!context.performed) { return; }
        OnProceed?.Invoke();
        Debug.Log("OnProceedイベントが呼び出されました！");
    }
}

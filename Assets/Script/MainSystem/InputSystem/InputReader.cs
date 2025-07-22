using System;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

// "Controls.IBattaleActionActions" が存在しないため、正しいインターフェース名に修正する必要があります。
// 通常、Input Systemの自動生成コードでは "Controls.IBattaleActionActions" ではなく "Controls.IBattleActionActions" などが使われます。
// まず "Controls" クラスの定義や自動生成されたコードを確認し、正しいインターフェース名を特定してください。
// ここでは仮に "IBattleActionActions" であると想定して修正します。

public class InputReader : MonoBehaviour, BattleControls.IBattaleActionActions
{
    public event Action DisCardEvent;

    private BattleControls controls;

    void Start()
    {
        controls = new BattleControls();
        controls.BattaleAction.SetCallbacks(this);
        controls.BattaleAction.Enable();
    }

    public void OnDisCard(InputAction.CallbackContext context)
    {
        //入力が「実行された瞬間」でなければ終了
        if (!context.performed) { return; }
        DisCardEvent?.Invoke();
    }

    public void OnCardSelectOne(InputAction.CallbackContext context)
    {
        throw new NotImplementedException();
    }

    public void OnCardSelectSelect(InputAction.CallbackContext context)
    {
        throw new NotImplementedException();
    }

    public void OnCardSelectTree(InputAction.CallbackContext context)
    {
        throw new NotImplementedException();
    }
}

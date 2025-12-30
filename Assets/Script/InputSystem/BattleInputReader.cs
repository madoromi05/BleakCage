using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// カード選択と防御アクションの入力を処理するコンポーネント
/// </summary>
public class BattleInputReader : MonoBehaviour, InputControls.IBattleActionActions, InputControls.IDefenseActionActions
{
    //[SerializeField] private Text debugText;
    public event Action<int> CardSelectEvent;
    public event Action DisCardEvent;
    public event Action<int> OnDefend;
    public event Action<int> OnDefendCanceled;

    private InputControls controls;

    private void OnEnable()
    {
        if (controls == null)
        {
            controls = new InputControls();
            controls.BattleAction.SetCallbacks(this);
            controls.DefenseAction.SetCallbacks(this);
        }
        EnableBattleActionMap();
    }

    private void OnDisable()
    {
        controls?.BattleAction.Disable();
        controls?.DefenseAction.Disable();
    }

    /// <summary>
    /// プレイヤーのカード選択入力だけを許可する
    /// </summary>
    public void EnableBattleActionMap()
    {
        controls.DefenseAction.Disable();
        controls.BattleAction.Enable();
    }

    /// <summary>
    /// 敵の攻撃に対する防御入力だけを許可する
    /// </summary>
    public void EnableDefenseActionMap()
    {
        controls.BattleAction.Disable();
        controls.DefenseAction.Enable();
    }

    /// <summary>
    /// 全てのバトル/防御入力を無効にする
    /// </summary>
    public void DisableAllActionMaps()
    {
        controls?.BattleAction.Disable();
        controls?.DefenseAction.Disable();
    }

    // --- BattleAction (カード選択) ---
    public void OnCardSelectOne(InputAction.CallbackContext context)
    {
        if (!context.ReadValueAsButton()) { return; }
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

    public void OnDisCard(InputAction.CallbackContext context)
    {
        if (!context.performed) { return; }
        DisCardEvent?.Invoke();
    }

    /// <summary>
    /// DefenseAction (防御/カウンター)
    /// </summary>
    /// <param name="context"></param>
    public void OnDefenseOne(InputAction.CallbackContext context)
    {
        // キーが押された瞬間に OnDefend を発行
        if (context.performed)
        {
            OnDefend?.Invoke(1);
        }
        // キーが離された瞬間に OnDefendCanceled を発行
        else if (context.canceled)
        {
            OnDefendCanceled?.Invoke(1);
        }
    }

    public void OnDefenseTwo(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnDefend?.Invoke(2);
        }
        else if (context.canceled)
        {
            OnDefendCanceled?.Invoke(2);
        }
    }

    public void OnDefenseTree(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnDefend?.Invoke(3);
        }
        else if (context.canceled)
        {
            OnDefendCanceled?.Invoke(3);
        }
    }
    public void OnDefenseFour(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnDefend?.Invoke(4);
        else if (context.canceled)
            OnDefendCanceled?.Invoke(4);
    }

}
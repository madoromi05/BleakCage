using UnityEngine;
using System;

public class EnemyHPHandler
{
    // 修正: プライベート変数は _ + camelCase
    private readonly EnemyRuntime _ownerRuntime;

    public event Action<EnemyRuntime> OnDead;

    public EnemyHPHandler(EnemyRuntime ownerRuntime)
    {
        _ownerRuntime = ownerRuntime;
    }

    public void TakeDamage(float damage)
    {
        // ★デバッグ用ログ1: メソッドが呼ばれたか確認
        Debug.Log($"[EnemyHPHandler] TakeDamage Called: Target={_ownerRuntime.EnemyModel.EnemyName}, Damage={damage}, CurrentHP={_ownerRuntime.CurrentHP}");

        if (_ownerRuntime.CurrentHP <= 0) return;

        // 【援護(Cover)】チェック
        if (_ownerRuntime.StatusHandler.GetStackCount(StatusEffectType.Cover) > 0)
        {
            Debug.Log($"[{_ownerRuntime.EnemyModel.EnemyName}] 【援護】発動！ダメージ無効化。");
            _ownerRuntime.StatusHandler.ConsumeStack(StatusEffectType.Cover, 1);
            return;
        }

        // 通常ダメージ処理
        _ownerRuntime.CurrentHP -= damage;

        //  HPが減ったか確認
        // Debug.Log($"[EnemyHPHandler] HP Updated: NewHP={_ownerRuntime.CurrentHP}");

        if (_ownerRuntime.CurrentHP <= 0)
        {
            _ownerRuntime.CurrentHP = 0;

            // ★デバッグ用ログ3: イベント発火直前
            Debug.Log($"[EnemyHPHandler] {_ownerRuntime.EnemyModel.EnemyName} is DEAD. Invoking OnDead event...");

            OnDead?.Invoke(_ownerRuntime);
        }
    }
}
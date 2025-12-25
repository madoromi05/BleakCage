using UnityEngine;
using System; // Actionのために追加

public class EnemyHPHandler
{
    private readonly EnemyRuntime ownerRuntime;

    // 死亡時に通知するイベント
    public event Action<EnemyRuntime> OnDead;

    public EnemyHPHandler(EnemyRuntime ownerRuntime)
    {
        this.ownerRuntime = ownerRuntime;
    }

    public void TakeDamage(float damage)
    {
        if (ownerRuntime.CurrentHP <= 0) return;

        // 【援護(Cover)】チェック
        if (ownerRuntime.StatusHandler.GetStackCount(StatusEffectType.Cover) > 0)
        {
            Debug.Log($"[{ownerRuntime.EnemyModel.EnemyName}] 【援護】発動！ダメージ無効化。");
            ownerRuntime.StatusHandler.ConsumeStack(StatusEffectType.Cover, 1);
            return;
        }

        // 通常ダメージ処理
        ownerRuntime.CurrentHP -= damage;

        if (ownerRuntime.CurrentHP <= 0)
        {
            ownerRuntime.CurrentHP = 0;
            // HPが0になったらイベント発火
            OnDead?.Invoke(ownerRuntime);
        }
    }
}
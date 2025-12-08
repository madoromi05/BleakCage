using UnityEngine;

public class EnemyHPHandler
{
    private readonly EnemyRuntime ownerRuntime;

    public EnemyHPHandler(EnemyRuntime ownerRuntime)
    {
        this.ownerRuntime = ownerRuntime;
    }

    /// <summary>
    /// ダメージを受ける処理
    /// </summary>
    public void TakeDamage(float damage)
    {
        // 【援護(Cover)】チェック
        if (ownerRuntime.CurrentHP <= 0) return;
        if (ownerRuntime.StatusHandler.GetStackCount(StatusEffectType.Cover) > 0)
        {
            Debug.Log($"[{ownerRuntime.EnemyModel.EnemyName}] 【援護】発動！ダメージ無効化。");

            // スタックを消費してダメージを0にする
            ownerRuntime.StatusHandler.ConsumeStack(StatusEffectType.Cover, 1);
            return;
        }

        // 通常ダメージ処理
        // RuntimeのCurrentHPを減算する
        ownerRuntime.CurrentHP -= damage;

        if (ownerRuntime.CurrentHP < 0) ownerRuntime.CurrentHP = 0;
    }
}
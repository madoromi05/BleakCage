using UnityEngine;

public class EnemyHPHandler
{
    private readonly EnemyModel ownerModel;

    public EnemyHPHandler(EnemyModel ownerModel)
    {
        this.ownerModel = ownerModel;
    }

    /// <summary>
    /// ダメージを受ける処理
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (ownerModel.EnemyHP <= 0) return;

        // 【援護】チェック
        // スタックがあれば消費してダメージを0にする
        if (ownerModel.StatusHandler.GetStackCount(StatusEffectType.Cover) > 0)
        {
            Debug.Log($"[{ownerModel.EnemyName}] 【援護】発動！ダメージ無効化。");
            ownerModel.StatusHandler.ConsumeStack(StatusEffectType.Cover, 1);
            return;
        }

        // 通常ダメージ処理
        ownerModel.EnemyHP -= damage;
        if (ownerModel.EnemyHP < 0) ownerModel.EnemyHP = 0;
    }
}
using UnityEngine;

public static class BattleCalculator
{
    /// <summary>
    /// ダメージ計算（破砕、熔鉄などを考慮）
    /// </summary>
    public static float CalculateDamage(
        float attackPower,
        float targetBaseDefense,
        float cardPenetration,
        StatusEffectHandler targetStatus) // 攻撃される側の状態
    {
        // 1. 【破砕】: スタック数 * 20% の貫通加算
        int fractureStacks = targetStatus.GetStackCount(StatusEffectType.Fracture);
        float extraPenetration = fractureStacks * 0.20f;
        float totalPenetration = Mathf.Clamp01(cardPenetration + extraPenetration);

        // 2. 【熔鉄】: 防御ダウン (1スタック5%)
        int meltdownStacks = targetStatus.GetStackCount(StatusEffectType.Meltdown);
        float defMultiplier = Mathf.Max(0, 1.0f - (0.05f * meltdownStacks));

        float currentDefense = targetBaseDefense * defMultiplier;

        // 3. 最終防御力（貫通適用）
        float effectiveDefense = currentDefense * (1.0f - totalPenetration);
        
        // 4. ダメージ確定（最低1）
        float damage = Mathf.Max(1, attackPower - effectiveDefense);
        return damage;
    }

    /// <summary>
    /// 敵の攻撃力計算（熔鉄による攻撃ダウンを考慮）
    /// </summary>
    public static float CalculateEnemyAttackPower(float baseAttack, StatusEffectHandler enemyStatus)
    {
        // 【熔鉄】: 攻撃ダウン (1スタック10%)
        int meltdownStacks = enemyStatus.GetStackCount(StatusEffectType.Meltdown);
        float atkMultiplier = Mathf.Max(0, 1.0f - (0.10f * meltdownStacks));

        return baseAttack * atkMultiplier;
    }

    /// <summary>
    /// 命中率計算（目標を考慮）
    /// </summary>
    public static float CalculateHitRate(float baseHitRate, StatusEffectHandler targetStatus)
    {
        // 【目標】: 命中UP (1スタック5%)
        int targetStacks = targetStatus.GetStackCount(StatusEffectType.Target);
        float bonusHitRate = targetStacks * 0.05f;

        return Mathf.Clamp01(baseHitRate + bonusHitRate);
    }
}
using UnityEngine;

/// <summary>
/// 弱点属性によるダメージ計算システム
/// 攻撃する側 -> Attacker
/// 攻撃を受ける側 -> Defender
/// </summary>
public class EnemyAttackDamage : IEnemyAttackStrategy
{
    [Header("難易度によって変わる値")]
    private float decayAdjustment = 1.0f;

    /// <summary>
    /// ダメージ計算を実行
    /// </summary>
    public float CalculateFinalDamage(EnemyModel attacker, PlayerModel target)
    {
        // 1. 効率ηを計算
        float efficiency = CalculateEfficiency(
            attacker.EnemyAttackPower, // 攻撃側のパワー
            target.PlayerDefensePower
        );

        // 2. 最終ダメージを計算
        float finalDamage = attacker.EnemyAttackPower * efficiency;
        return finalDamage;
    }

    /// <summary>
    /// 効率ηを計算
    /// </summary>
    private float CalculateEfficiency(float attackerPower, float defenderPower)
    {
        // 分子: (R^P) * (D*R)^2
        float numerator = Mathf.Pow(defenderPower, 2);

        // 分母: AP * P * d
        // ゼロ除算を避ける
        float denominator = attackerPower * decayAdjustment;
        if (Mathf.Approximately(denominator, 0)) return 0f;

        float calculatedEfficiency = 1f - (numerator / denominator);

        // 効率は0から1の範囲にクランプ
        return Mathf.Clamp01(calculatedEfficiency);
    }
}

using UnityEngine;

/// <summary>
/// 弱点属性によるダメージ計算システム
/// 攻撃する側 -> Attacker
/// 攻撃を受ける側 -> Defender
/// </summary>
public class AttributeWeakness : IAttackStrategy
{
    [Header("難易度によって変わる値")]
    private float correlationCoefficient = 1.5f;                    // 相関係数 (デフォルト値)
    private float decayAdjustment = 1.0f;                           // 減衰調整 (デフォルト値)

    /// <summary>
    /// ダメージ計算を実行
    /// これを基本的に呼び出す
    /// </summary>
    public float CalculateFinalDamage(PlayerRuntime attacker, WeaponRuntime weapon, CardRuntime cardRuntime,EnemyModel target)
    {
        // 1. 相性係数を取得
        float relationCoefficient = GetRelationCoefficient(weapon.Attribute, target.EnemyDefensAttribute);

        // 2. 効率ηを計算
        float efficiency = CalculateEfficiency(
            relationCoefficient,
            weapon.PeakyCoefficient,
            attacker.GetPower(), // 攻撃側のパワー
            weapon.GetPower(),   // 武器のパワー
            target.EnemyDefensePower
        );

        // 3. 最終ダメージを計算
        float finalDamage = weapon.GetPower() * efficiency * cardRuntime.GetOutput();
        return finalDamage;
    }

    /// <summary>
    /// 効率ηを計算
    /// 計算式: η = 1 - (R^P) (DR)^2 / (AP * C * d)
    /// R = 相性係数
    /// P = ピーキー係数
    /// D = 防御力
    /// AP = 攻撃力
    /// C = 相関係数
    /// d = 減衰調整
    /// </summary>
    private float CalculateEfficiency(float relationCoefficient, float peakyCoefficient, float attackerPower, float weaponPower, float defenderPower)
    {
        // 分子: (R^P) * (D*R)^2
        float numerator = Mathf.Pow(relationCoefficient, peakyCoefficient) * Mathf.Pow(defenderPower * relationCoefficient, 2);

        // 分母: AP * P * d
        // ゼロ除算を避ける
        float denominator = attackerPower * peakyCoefficient * decayAdjustment;
        if (Mathf.Approximately(denominator, 0)) return 0f;

        float calculatedEfficiency = 1f - (numerator / denominator);

        // 効率は0から1の範囲にクランプ
        return Mathf.Clamp01(calculatedEfficiency);
    }

    /// <summary>
    /// 攻撃属性と敵の種類から相性係数を取得
    /// </summary>
    private float GetRelationCoefficient(AttributeType attackAttr, DefensAttributeType enemyAttr)
    {
        // 相性係数のデフォルト値 (普通の場合)
        float coefficient = 1f;

        switch (attackAttr)
        {
            case AttributeType.Slash: // 斬
                if(enemyAttr == DefensAttributeType.Repulsive) coefficient = correlationCoefficient;            // 斥力:有利
                else if(enemyAttr == DefensAttributeType.Hardness) coefficient = 1f / correlationCoefficient;   // 堅牢:不利
                break;

            case AttributeType.Blunt: // 鈍
                if(enemyAttr == DefensAttributeType.Softness) coefficient = correlationCoefficient;             // 軟体:有利
                else if(enemyAttr == DefensAttributeType.Repulsive) coefficient = 1f / correlationCoefficient;  // 斥力:不利
                break;

            case AttributeType.Pierce: // 突
                if (enemyAttr == DefensAttributeType.Hardness) coefficient = correlationCoefficient;            // 堅牢:有利
                else if(enemyAttr == DefensAttributeType.Softness) coefficient = 1f / correlationCoefficient;   // 軟体:不利
                break;

            case AttributeType.Bullet: // 弾
                // 弾属性は全て普通 (デフォルト値)
                break;
        }

        return coefficient;
    }
}
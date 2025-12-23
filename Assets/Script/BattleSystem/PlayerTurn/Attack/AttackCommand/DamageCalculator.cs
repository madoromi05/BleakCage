using UnityEngine;

/// <summary>
/// 弱点属性によるダメージ計算システム
/// </summary>
public class DamageCalculator : MonoBehaviour
{
    [Header("属性相性")]
    [SerializeField] private float attributeMultiplier = 1.5f;
    [SerializeField] private float k = 1.0f;
    [SerializeField] private float attenuationConstant = 100f;

    /// <summary>
    /// 最終的なダメージを計算して返すメインメソッド
    /// </summary>
    public float CalculateFinalDamage(PlayerRuntime player, WeaponRuntime weapon, CardRuntime card, EnemyRuntime enemy)
    {
        float A = player.Level;
        float O = weapon.attackPower;
        float Co = card.GetOutput();
        float D = enemy.EnemyModel.EnemyDefensePower;
        float Cp = card.DefensePenetration;

        float attackMultiplier = GetPlayerAttackMultiplier(player);
        float baseAttack = (A + O) * Co * attackMultiplier;

        // 敵の状態異常（防御・貫通補正）を適用
        // D と Cp は参照渡し (ref) で書き換える
        ApplyDefenseModifiers(enemy, ref D, ref Cp);

        // 減衰率を計算
        float attenuationRate = CalculateAttenuationRate(D, Cp);

        // 属性相性倍率を取得
        float R = GetRelationCoefficient(weapon.Attribute, enemy.EnemyModel.EnemyDefensAttribute);
        float P = weapon.PeakyCoefficient;
        float affinityMultiplier = Mathf.Pow(R, P);

        // 最終計算
        return baseAttack * attenuationRate * affinityMultiplier;
    }

    /// <summary>
    /// プレイヤーのステータス効果から攻撃力倍率を算出
    /// </summary>
    private float GetPlayerAttackMultiplier(PlayerRuntime player)
    {
        if (player.StatusHandler == null) return 1.0f;

        float multiplier = 1.0f;

        // 【攻撃力UP】: 1スタックにつき +10%
        int atkStacks = player.StatusHandler.GetStackCount(StatusEffectType.AttackUp);
        multiplier += (atkStacks * 0.1f);

        return multiplier;
    }

    /// <summary>
    /// 敵のステータス効果に基づいて防御力(D)と貫通力(Cp)を補正する
    /// </summary>
    private void ApplyDefenseModifiers(EnemyRuntime enemy, ref float D, ref float Cp)
    {
        if (enemy.StatusHandler == null) return;

        // 【防御力UP】: 1スタックにつき防御力 +20%
        int defStacks = enemy.StatusHandler.GetStackCount(StatusEffectType.DefenceUp);
        if (defStacks > 0)
        {
            D *= (1.0f + defStacks * 0.2f);
        }

        // 【破砕】: 1スタックにつき貫通(Cp) +0.2
        int fracture = enemy.StatusHandler.GetStackCount(StatusEffectType.Fracture);
        if (fracture > 0)
        {
            Cp += (fracture * 0.2f);
        }

        // 【熔鉄】: 1スタックにつき防御(D) -5%
        int meltdown = enemy.StatusHandler.GetStackCount(StatusEffectType.Meltdown);
        if (meltdown > 0)
        {
            D *= Mathf.Max(0, 1.0f - (0.05f * meltdown));
        }
    }

    /// <summary>
    /// 減衰率の計算ロジック
    /// </summary>
    private float CalculateAttenuationRate(float D, float Cp)
    {
        // 実質的防御力の計算 (ゼロ除算回避)
        float effectiveDefense = Mathf.Approximately(Cp, 0) ? D : D / Cp;

        float kCa = k * attenuationConstant;
        float denominator = kCa + effectiveDefense;

        if (Mathf.Approximately(denominator, 0)) return 0f;

        return Mathf.Clamp01(kCa / denominator);
    }

    /// <summary>
    /// 属性相性係数の取得（既存処理）
    /// </summary>
    private float GetRelationCoefficient(AttributeType attackAttr, DefensAttributeType enemyAttr)
    {
        float coefficient = 1f;

        switch (attackAttr)
        {
            case AttributeType.Slash:
                if (enemyAttr == DefensAttributeType.Repulsive) coefficient = attributeMultiplier;
                else if (enemyAttr == DefensAttributeType.Hardness) coefficient = 1f / attributeMultiplier;
                break;

            case AttributeType.Blunt:
                if (enemyAttr == DefensAttributeType.Softness) coefficient = attributeMultiplier;
                else if (enemyAttr == DefensAttributeType.Repulsive) coefficient = 1f / attributeMultiplier;
                break;

            case AttributeType.Pierce:
                if (enemyAttr == DefensAttributeType.Hardness) coefficient = attributeMultiplier;
                else if (enemyAttr == DefensAttributeType.Softness) coefficient = 1f / attributeMultiplier;
                break;

            case AttributeType.Bullet:
                break;
        }

        return coefficient;
    }
}
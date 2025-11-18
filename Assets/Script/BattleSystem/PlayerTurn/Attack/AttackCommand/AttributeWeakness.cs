using UnityEngine;

/// <summary>
/// 弱点属性によるダメージ計算システム (新計算式)
/// 攻撃する側 -> Attacker
/// 攻撃を受ける側 -> Defender
/// </summary>
public class AttributeWeakness : IAttackStrategy
{
    [Header("属性相性")]
    [SerializeField] private float attributeMultiplier = 1.5f;      // 属性倍率 (デフォルト値)

    [Header("新ダメージ計算式用パラメータ")]
    [Tooltip("減衰率のスケーリング係数 (k)")]
    [SerializeField] private float k = 1.0f;
    [Tooltip("減衰定数 (Ca)。この値が実質的防御力と等しい時、減衰率が50%になる")]
    [SerializeField] private float attenuationConstant = 100f;

    /// <summary>
    /// ダメージ計算を実行
    /// 計算式: E = (基礎攻撃力) * (減衰率) * (相性倍率)
    /// - 基礎攻撃力 = (A + O) * Co
    /// - 減衰率 = (k * Ca) / (k * Ca + (D / Cp))
    /// - 相性倍率 = R^P
    /// </summary>
    public float CalculateFinalDamage(PlayerRuntime player, WeaponRuntime weapon, CardRuntime card, EnemyModel enemy)
    {
        float A = player.Level;
        float O = weapon.attackPower;
        float Co = card.GetOutput();
        float D = enemy.EnemyDefensePower;
        float Cp = card.DefensePenetration;
        float R = GetRelationCoefficient(weapon.Attribute, enemy.EnemyDefensAttribute);
        float P = weapon.PeakyCoefficient;

        // --- 1. 基礎攻撃力 ((A + O) * Co) の計算 ---
        float baseAttack = (A + O) * Co;

        // --- 2. 減衰率 ((k * Ca) / (k * Ca + (D / Cp))) の計算 ---

        // (D / Cp) (実質的防御力)
        float effectiveDefense;
        if (Mathf.Approximately(Cp, 0))
        {
            effectiveDefense = D;
        }
        else
        {
            effectiveDefense = D / Cp;
        }

        // (k * Ca)
        float kCa = k * attenuationConstant;

        // 分母 (k * Ca + 実質的防御力)
        float denominator = kCa + effectiveDefense;

        // 減衰率の計算
        float attenuationRate;
        if (effectiveDefense == float.MaxValue)
        {
            attenuationRate = 0f; // 分母が無限大
        }
        else if (Mathf.Approximately(denominator, 0))
        {
            // kCaとeffectiveDefenseが両方0の場合など
            Debug.LogWarning("減衰率の分母が0です。");
            attenuationRate = 0f;
        }
        else
        {
            // 減衰率 = (k * Ca) / (k * Ca + D / Cp)
            attenuationRate = kCa / denominator;
        }

        // 念のため 0..1 の範囲にクランプ
        attenuationRate = Mathf.Clamp01(attenuationRate);

        // --- 3. 相性倍率 (R^P) の計算 ---
        float affinityMultiplier = Mathf.Pow(R, P);


        // --- 4. 最終ダメージ (E) の計算 ---
        // E = 基礎攻撃力 * 減衰率 * 相性倍率
        float finalDamage = baseAttack * attenuationRate * affinityMultiplier;

        // --- (Debug Log) ---
        Debug.Log("------------------------- 新ダメージ計算開始 -------------------------");
        Debug.Log($"入力値: A(Lv)={A}, O(武器)={O}, Co(カード)={Co}, D(敵防御)={D}, Cp(貫通)={Cp}, R(相性)={R}, P(ピーキー)={P}");
        Debug.Log($"定数: k={k}, Ca={attenuationConstant}");
        Debug.Log($"[ステップ1] 基礎攻撃力: ({A} + {O}) * {Co} = {baseAttack}");
        Debug.Log($"[ステップ2] 実質的防御力(D/Cp): {D} / {Cp} = {effectiveDefense}");
        Debug.Log($"[ステップ3] 減衰率(kCa / (kCa + D/Cp)): ({kCa}) / ({kCa} + {effectiveDefense}) = {attenuationRate}");
        Debug.Log($"[ステップ4] 相性倍率(R^P): {R}^{P} = {affinityMultiplier}");
        Debug.Log($"<b>[最終結果] Final Damage: {baseAttack} * {attenuationRate} * {affinityMultiplier} = {finalDamage}</b>");
        Debug.Log("------------------------- 新ダメージ計算終了 -------------------------");

        return finalDamage;
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
            case AttributeType.Slash:
                if (enemyAttr == DefensAttributeType.Repulsive) coefficient = attributeMultiplier;        // 斥力:有利
                else if (enemyAttr == DefensAttributeType.Hardness) coefficient = 1f / attributeMultiplier;  // 堅牢:不利
                break;

            case AttributeType.Blunt:
                if (enemyAttr == DefensAttributeType.Softness) coefficient = attributeMultiplier;      // 軟体:有利
                else if (enemyAttr == DefensAttributeType.Repulsive) coefficient = 1f / attributeMultiplier; // 斥力:不利
                break;

            case AttributeType.Pierce:
                if (enemyAttr == DefensAttributeType.Hardness) coefficient = attributeMultiplier;       // 堅牢:有利
                else if (enemyAttr == DefensAttributeType.Softness) coefficient = 1f / attributeMultiplier;  // 軟体:不利
                break;

            case AttributeType.Bullet:
                // 弾属性は全て普通 (デフォルト値)
                break;
        }

        return coefficient;
    }
}
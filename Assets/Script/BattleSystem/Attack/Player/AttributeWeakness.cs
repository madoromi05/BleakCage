using UnityEngine;

/// <summary>
/// 弱点属性によるダメージ計算システム
/// 攻撃する側 -> Attacker
/// 攻撃を受ける側 -> Defender
/// </summary>
public class AttributeWeakness : IAttackStrategy
{
    [Header("難易度によって変わる値")]
    private float attributeMultiplier = 1.5f;                   // 属性倍率 (デフォルト値)
    private float damageScale = 1.0f;                           // 減衰調整 (デフォルト値)

    private const float ATTACK_COEFFICIENT_X = 10f;             // ダメージにかける変数
    /// <summary>
    /// ダメージ計算を実行
    /// これを基本的に呼び出す
    /// </summary>
    public float CalculateFinalDamage(PlayerRuntime player, WeaponRuntime weapon, CardRuntime card,EnemyModel enemy)
    {
        Debug.Log("--- ダメージ計算開始 ---");
        Debug.Log($"プレイヤーLv: {player.Level}, 武器攻撃力: {weapon.attackPower}, カード出力: {card.GetOutput()}");
        Debug.Log($"敵防御力: {enemy.EnemyDefensePower}, 敵防御属性: {enemy.EnemyDefensAttribute}");

        // 1. 属性倍率を取得
        float relationCoefficient = GetRelationCoefficient(weapon.Attribute, enemy.EnemyDefensAttribute);
        Debug.Log($"[ステップ1] 属性相性係数(R): {relationCoefficient}");

        // 2. 効率ηを計算
        float efficiency = CalculateEfficiency(
            relationCoefficient,
            weapon.PeakyCoefficient,
            player.Level,      // 攻撃側のパワー
            weapon.attackPower,   // 武器のパワー
            enemy.EnemyDefensePower,
            card.DefensePenetration
        );

        // 3. 最終ダメージを計算
        float finalDamage = weapon.attackPower * efficiency * card.GetOutput();
        Debug.Log($"<b>[最終結果] Final Damage: {weapon.attackPower} * {efficiency} * {card.GetOutput()} = {finalDamage}</b>");
        Debug.Log("--- ダメージ計算終了 ---");

        return finalDamage;
    }

    /// <summary>
    /// ダメージ効率ηを計算します。
    /// 計算式: η = 1 - ( (R^P) * (D*R)^2 ) / (L * X * P * d)
    /// </summary>
    /// <param name="relationCoefficient">R: 相性係数</param>
    /// <param name="peakyCoefficient">P: ピーキー係数</param>
    /// <param name="playerLevel">L: 攻撃側のレベル</param>
    /// <param name="defenderPower">D: 防御側の防御力</param>
    /// <returns>0から1の範囲に丸められたダメージ効率</returns>
    private float CalculateEfficiency(float relationCoefficient, float peakyCoefficient,
            float playerLevel, float weaponPower, float defenderPower, float defensePenetration)
    {
        Debug.Log("--- 効率ηの計算開始 ---");
        Debug.Log($"入力値: R={relationCoefficient}, P(ピーキー係数)={peakyCoefficient}, L={playerLevel}, D={defenderPower}, 貫通率={defensePenetration}");

        float penetratedDefenderPower = defenderPower * (1f - Mathf.Clamp01(defensePenetration));
        Debug.Log($"貫通適用後の防御力: {penetratedDefenderPower}");


        // (D*R*Cp)^2 の部分
        float relationPoweredByPeaky = Mathf.Pow(relationCoefficient, peakyCoefficient);
        float defenderEffectiveness = penetratedDefenderPower * relationCoefficient;
        float defenderEffectivenessSquared = Mathf.Pow(defenderEffectiveness, 2);
        float numerator = relationPoweredByPeaky * defenderEffectivenessSquared;
        Debug.Log($"分子: (R^P) * (D*R)^2 = {numerator}");

        // --- 分母の計算: L * X * P * d ---
        // L: playerLevel, X: ATTACK_COEFFICIENT_X, P: peakyCoefficient, d: damageScale
        float denominator = playerLevel * ATTACK_COEFFICIENT_X * peakyCoefficient * damageScale;
        Debug.Log($"分母: L * X * P * d = {denominator}");

        // ゼロ除算を防止
        if (Mathf.Approximately(denominator, 0))
        {
            Debug.LogError("分母が0になりました。playerLevelまたはpeakyCoefficientが0の可能性があります。");
            return 0f;
        }

        // --- 効率(η)の計算 ---
        float calculatedEfficiency = 1f - (numerator / denominator);
        Debug.Log($"効率η (クランプ前): 1 - ({numerator} / {denominator}) = {calculatedEfficiency}");

        float finalEfficiency = Mathf.Clamp01(calculatedEfficiency);
        Debug.Log($"<b>[ステップ2] 最終的な効率η (0-1にクランプ後): {finalEfficiency}</b>");
        Debug.Log("--- 効率ηの計算終了 ---");

        // 効率がマイナスにならないよう、0から1の範囲に値を制限（クランプ）します。
        return finalEfficiency;
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
                if (enemyAttr == DefensAttributeType.Repulsive) coefficient = attributeMultiplier;            // 斥力:有利
                else if (enemyAttr == DefensAttributeType.Hardness) coefficient = 1f / attributeMultiplier;   // 堅牢:不利
                break;

            case AttributeType.Blunt:
                if(enemyAttr == DefensAttributeType.Softness) coefficient = attributeMultiplier;             // 軟体:有利
                else if(enemyAttr == DefensAttributeType.Repulsive) coefficient = 1f / attributeMultiplier;  // 斥力:不利
                break;

            case AttributeType.Pierce:
                if (enemyAttr == DefensAttributeType.Hardness) coefficient = attributeMultiplier;            // 堅牢:有利
                else if(enemyAttr == DefensAttributeType.Softness) coefficient = 1f / attributeMultiplier;   // 軟体:不利
                break;

            case AttributeType.Bullet:
                // 弾属性は全て普通 (デフォルト値)
                break;
        }

        return coefficient;
    }
}
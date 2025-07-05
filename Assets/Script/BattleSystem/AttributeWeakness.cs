using UnityEngine;

/// <summary>
/// 弱点属性によるダメージ計算システム
/// 攻撃する側 -> Attacker
/// 攻撃を受ける側 -> Defender
/// </summary>
public class AttributeWeakness : MonoBehaviour
{
    [Header("属性設定")]
    public AttributeData.Attribute weakAgainstAttribute;               // 弱点属性
    public AttributeData.CharacterAttribute weakAgainstCharacterType;  // 敵の種類

    [Header("計算")]
    public float finalDamage;       // 最終ダメージ
    public float efficiency;        // 攻撃効率

    [Header("動的な値")]
    public float attackerPower;     // 攻撃側のパワー
    public float weaponPower;       // 武器の基本パワー
    public float peakyCoefficient;  // ピーキー係数
    public float defenderPower;     // 防御側のパワー

    [Header("難易度によって変わる値")]
    public float correlationCoefficient = 1.5f;  // 相関係数 (デフォルト値)
    public float outputAdjustment = 1.0f;        // 出力調整 (デフォルト値)
    public float decayAdjustment = 1.0f;         // 減衰調整 (デフォルト値)

    /// <summary>
    /// ダメージ計算を実行
    /// これを基本的に呼び出す
    /// </summary>
    public void CalculateDamage()
    {
        // 相性係数を取得
        float relationCoefficient = GetRelationCoefficient(weakAgainstAttribute, weakAgainstCharacterType);

        // 効率ηを計算
        efficiency = CalculateEfficiency(relationCoefficient);

        // 最終ダメージを計算
        finalDamage = weaponPower * efficiency * outputAdjustment;
    }

    /// <summary>
    /// 効率ηを計算
    /// 計算式: η = 1 - (R^P) (DR)^2 / (AP * C * d)
    /// ここで:
    /// R = 相性係数
    /// P = ピーキー係数
    /// D = 防御力
    /// AP = 攻撃力
    /// C = 相関係数
    /// d = 減衰調整
    /// </summary>
    private float CalculateEfficiency(float relationCoefficient)
    {
        float numerator = Mathf.Pow(relationCoefficient, peakyCoefficient) *
                         Mathf.Pow(defenderPower * relationCoefficient, 2);
        float denominator = attackerPower * peakyCoefficient * decayAdjustment;

        float calculatedEfficiency = 1f - (numerator / denominator);

        // 効率は0から1の範囲にクランプ
        return Mathf.Clamp01(calculatedEfficiency);
    }

    /// <summary>
    /// 攻撃属性と敵の種類から相性係数を取得
    /// </summary>
    private float GetRelationCoefficient(AttributeData.Attribute attackAttr, AttributeData.CharacterAttribute enemyAttr)
    {
        // 相性係数のデフォルト値 (普通の場合)
        float coefficient = 1f;

        switch (attackAttr)
        {
            case AttributeData.Attribute.Slash: // 斬
                switch (enemyAttr)
                {
                    case AttributeData.CharacterAttribute.Repulsive: // 斥力:有利
                        coefficient = correlationCoefficient;
                        break;
                    case AttributeData.CharacterAttribute.Sturdy:    // 堅牢:不利
                        coefficient = 1f / correlationCoefficient;
                        break;
                        // 軟体:普通 (デフォルト値)
                }
                break;

            case AttributeData.Attribute.Blunt: // 鈍
                switch (enemyAttr)
                {
                    case AttributeData.CharacterAttribute.Soft:      // 軟体:有利
                        coefficient = correlationCoefficient;
                        break;
                    case AttributeData.CharacterAttribute.Repulsive: // 斥力:不利
                        coefficient = 1f / correlationCoefficient;
                        break;
                        // 堅牢:普通 (デフォルト値)
                }
                break;

            case AttributeData.Attribute.Pierce: // 突
                switch (enemyAttr)
                {
                    case AttributeData.CharacterAttribute.Sturdy:    // 堅牢:有利
                        coefficient = correlationCoefficient;
                        break;
                    case AttributeData.CharacterAttribute.Soft:      // 軟体:不利
                        coefficient = 1f / correlationCoefficient;
                        break;
                        // 斥力:普通 (デフォルト値)
                }
                break;

            case AttributeData.Attribute.Bullet: // 弾
                // 弾属性は全て普通 (デフォルト値)
                break;
        }

        return coefficient;
    }
}
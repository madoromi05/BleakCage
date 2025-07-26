using UnityEngine;

/// <summary>
/// 実行時に使用されるカードのデータモデル。
/// CardEntity（ScriptableObject）から初期化される。
/// </summary>
public class CardModel
{
    public int CardId { get; private set; }
    public string CardName { get; private set; }
    public CardEntity.CardTypeData CardType { get; private set; }
    public AttributeType CardAttribute { get; private set; }

    public int AttackCount { get; private set; }                                // 攻撃回数
    public int TargetCount { get; private set; }                                // 攻撃対象数
    public bool IsPassive { get; private set; }                                 // パッシブ効果なのかどうか
    public float HitRate { get; private set; }                                  // 命中率(1～0)

    public float OutputModifier { get; private set; }                           // 出力調整
    public float DefensePenetration { get; private set; }                       // 防御貫通率

    public Sprite CardIcon { get; private set; }                                // CardのIcon
    public string CardDescription { get; private set; }                         // Cardの説明文
    public ICardRestrictionStrategy RestrictionStrategy { get; private set; }   // Card特定の物に装備させるためのもの

    /// <summary>
    /// CardEntity からデータを読み取って CardModel を生成する
    /// </summary>
    /// <param name="entity">ScriptableObjectから読み込んだCardEntity</param>
    public CardModel(CardEntity entity)
    {
        if (entity == null)
        {
            Debug.LogError("CardEntity is null.");
            return;
        }

        CardId = entity.CardId;
        CardName = entity.CardName;
        CardIcon = entity.CardIcon;
        CardDescription = entity.CardDescription;

        CardType = entity.CardType;
        CardAttribute = entity.CardAttribute;

        AttackCount = entity.CardAttackCount;
        TargetCount = entity.CardTargetCount;
        IsPassive = entity.CardPassive;

        HitRate = entity.CardHitRate;
        OutputModifier = entity.CardOutputModifier;
        DefensePenetration = entity.CardDefensePenetration;
    }
}
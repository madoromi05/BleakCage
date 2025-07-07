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
    public AttackAttributeType CardAttribute { get; private set; }

    public int AttackCount { get; private set; }
    public int TargetCount { get; private set; }
    public bool IsPassive { get; private set; }

    public float HitRate { get; private set; }
    public float OutputModifier { get; private set; }
    public float DefensePenetration { get; private set; }

    public Sprite CardIcon { get; private set; }
    public string CardDescription { get; private set; }

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
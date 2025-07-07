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

    public float AttackPower { get; private set; }
    public float HitRate { get; private set; }
    public float OutputModifier { get; private set; }
    public float DefensePenetration { get; private set; }

    public Sprite CardIcon { get; private set; }
    public string CardDescription { get; private set; }

    /// <summary>
    /// CardEntity からデータを読み取って CardModel を生成する
    /// </summary>
    /// <param name="entity">ScriptableObjectから読み込んだCardEntity</param>
    public CardModel(CardEntity cardEntity)
    {
        if (cardEntity == null)
        {
            Debug.LogError("CardEntity is null.");
            return;
        }

        CardId = cardEntity.CardId;
        CardName = cardEntity.CardName;
        CardType = cardEntity.CardType;
        CardAttribute = cardEntity.CardAttribute;

        AttackCount = cardEntity.CardAttackCount;
        TargetCount = cardEntity.CardTargetCount;
        IsPassive = cardEntity.CardPassive;

        AttackPower = cardEntity.CardAttackPower;
        HitRate = cardEntity.CardHitRate;
        OutputModifier = cardEntity.CardOutputModifier;
        DefensePenetration = cardEntity.CardDefensePenetration;

        CardIcon = cardEntity.CardIcon;
        CardDescription = cardEntity.CardDescription;
    }
}

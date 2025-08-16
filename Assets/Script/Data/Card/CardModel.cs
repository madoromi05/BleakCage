using UnityEngine;

/// <summary>
/// 実行時に使用されるカードのデータモデル。
/// CardEntity（ScriptableObject）から初期化される。
/// </summary>
public class CardModel
{
    public int ID { get; private set; }
    public string Name { get; private set; }
    public int[] EquipableWeaponIds { get; private set; } 
    public CardEntity.CardTypeData Type { get; private set; }
    public AttributeType Attribute { get; private set; }
    public int AttackCount { get; private set; }                            // 攻撃回数
    public int TargetCount { get; private set; }                            // 攻撃対象数
    public bool IsPassive { get; private set; }                             // パッシブ効果なのかどうか
    public float HitRate { get; private set; }                              // 命中率(1～0)
    public float OutputModifier { get; private set; }                       // 出力調整
    public float DefensePenetration { get; private set; }                   // 防御貫通率
    public Sprite Icon { get; private set; }                                // CardのIcon
    public string Description { get; private set; }                         // Cardの説明文

    /// <summary>
    /// CardEntity からデータを読み取って CardModel を生成する
    /// 読み取り専用にしている
    /// </summary>
    /// <param name="entity">ScriptableObjectから読み込んだCardEntity</param>
    public CardModel(CardEntity entity)
    {
        if (entity == null)
        {
            Debug.LogError("CardEntity is null.");
            return;
        }

        ID = entity.ID;
        Name = entity.Name;
        Icon = entity.Icon;
        EquipableWeaponIds = entity.EquipableWeaponID;
        Description = entity.Description;
        Type = entity.Type;
        Attribute = entity.Attribute;
        AttackCount = entity.AttackCount;
        TargetCount = entity.TargetCount;
        IsPassive = entity.Passive;
        HitRate = entity.HitRate;
        OutputModifier = entity.OutputModifier;
        DefensePenetration = entity.DefensePenetration;
    }
}
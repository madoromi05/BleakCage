using UnityEngine;

/// <summary>
/// 実行時に使用されるカードのデータモデル。
/// CardEntity（ScriptableObject）から初期化される。
/// </summary>
public class CardModel
{
    public int ID { get; private set; }
    public string Name { get; private set; }
    public int EquipableWeaponID { get; private set; }
    public int CharacterID { get; private set; }
    public AttributeType Attribute { get; private set; }
    public bool IsPassive { get; private set; }                             // パッシブ効果なのかどうか
    public float HitRate { get; private set; }                              // 命中率(1～0)
    public float OutputModifier { get; private set; }                       // 出力調整
    public float DefensePenetration { get; private set; }                   // 防御貫通率
    public string Description { get; private set; }                         // Cardの説明文
    public AnimationClip AttackAnimation { get; private set; }
    public StatusEffectData StatusEffect { get; private set; }
    public HandPosition WeaponHand { get; private set; }
    public bool IsMelee { get; private set; }
    public CardTargetScope TargetScope { get; private set; }
    public int TargetCount { get; private set; }
    public int AttackCount { get; private set; }
    public GameObject EffectPrefab { get; private set; }
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
        EquipableWeaponID = entity.EquipableWeaponID;
        CharacterID = entity.CharacterID;
        Description = entity.Description;
        Attribute = entity.Attribute;
        IsPassive = entity.Passive;
        HitRate = entity.HitRate;
        OutputModifier = entity.OutputModifier;
        DefensePenetration = entity.DefensePenetration;
        StatusEffect = entity.StatusEffect;
        AttackAnimation = entity.AttackAnimation;
        WeaponHand = entity.WeaponHand;
        IsMelee = entity.IsMelee;
        TargetScope = entity.TargetScope;
        TargetCount = entity.TargetCount;
        EffectPrefab = entity.EffectPrefab;
    }
}
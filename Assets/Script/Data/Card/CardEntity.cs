using UnityEngine;

/// <summary>
/// CSVから読み込むためのデータコンテナ
/// </summary>
[System.Serializable]
public struct StatusEffectData
{
    public StatusEffectType Type;
    public float Value;         // 効果量
    public int Duration;        // 持続ターン
    public int InflictStacks;   // 付与するスタック数
}

/// <summary>
/// スキルカードデータの定義
/// 編集をしやすくするために置いているだけなのでここからデータ参照はしない
/// </summary>

[CreateAssetMenu(fileName = "CardEntity", menuName = "Create CardEntity")]
public class CardEntity : ScriptableObject
{
    [Header("ID情報")]
    public int ID;                                                  // カード固有ID
    public int OwnerID;                                             // どの武器、キャラの専用カードなのか識別するID
    public int ExclusiveID;                                         // OwnerID内での識別ID

    [Header("基本情報")]
    public string Name;                                             // カード名
    public int CharacterID;                                         // このカードを装備するキャラID
    public int EquipableWeaponID;                                   // このカードを装備する武器ID
    public Sprite Icon;                                             // アイコン画像
    [Multiline]public string Description;                           // 説明文

    public CardTypeData Type;                                       // カードタイプ
    public AttributeType Attribute;                                 // 属性

    [Header("戦闘パラメータ")]
    public bool Passive;                                            // パッシブ効果判定
    public float HitRate;                                           // 命中率
    public float OutputModifier;                                    // 出力調整
    public float DefensePenetration;                                // 防御貫通 
    public CardTargetScope TargetScope = CardTargetScope.Single;    // 攻撃体数
    public bool IsMelee;                                            // 近接攻撃判定
    public int AttackCount;                                         // 攻撃回数
    public int TargetCount = 1;                                     // ランダム攻撃の対象数

    [Header("付与する異常状態")]
    public StatusEffectData StatusEffect;

    [Header("演出・挙動")]
    public AnimationClip AttackAnimation;
    public HandPosition WeaponHand;
}

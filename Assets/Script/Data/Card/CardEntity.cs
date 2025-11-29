using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// スキルカードデータの定義
/// 編集をしやすくするために置いているだけなのでここからデータ参照はしない
/// </summary>

[CreateAssetMenu(fileName = "CardEntity", menuName = "Create CardEntity")]
public class CardEntity : ScriptableObject
{
    public int ID;                          // カード固有ID
    public string Name;                     // カード名
    public int CharacterID;                 // このカードを装備するキャラID
    public int EquipableWeaponID;           // このカードを装備する武器ID
    public Sprite Icon;                     // アイコン画像
    public string Description;              // 説明文

    public CardTypeData Type;               // カードタイプ
    public AttributeType Attribute;         // 属性

    public int AttackCount;                 // 攻撃回数
    public int TargetCount;                 // 攻撃体数
    public bool Passive;                    // パッシブ効果判定

    public float HitRate;                   // 命中率
    public float OutputModifier;            // 出力調整
    public float DefensePenetration;        // 防御貫通 
    public AnimationClip AttackAnimation;
    public HandPosition WeaponHand;
    public bool IsMelee;
}

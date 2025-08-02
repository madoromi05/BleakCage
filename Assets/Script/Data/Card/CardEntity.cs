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
    public enum CardTypeData
    {
        Character,  // キャラ付き
        Weapon,     // 武器付き
        Universal   // 汎用
    }

    public int CardIdentifier;                  // カード固有ID
    public string CardName;                     // カード名
    public int[] EquippableWeaponIdentifier;    // 武器ID
    public Sprite CardIcon;                     // アイコン画像
    public string CardDescription;              // 説明文

    public CardTypeData CardType;               // カードタイプ
    public AttributeType CardAttribute;         // 属性

    public int CardAttackCount;                 // 攻撃回数
    public int CardTargetCount;                 // 攻撃体数
    public bool CardPassive;                    // パッシブ効果判定

    public float CardHitRate;                   // 命中率
    public float CardOutputModifier;            // 出力調整
    public float CardDefensePenetration;        // 防御貫通 
}

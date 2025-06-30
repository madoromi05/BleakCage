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
    public enum CardType
    {
        Character,  // キャラ付き
        Weapon,     // 武器付き
        Universal   // 汎用
    }

    public enum Attribute
    {
        Slash,      // 斬
        Blunt,      // 鈍
        Pierce,     // 突
        Bullet      // 弾
    }

    public int cardID;                  // カード固有ID
    public string cardName;             // カード名
    public CardType cardType;           // カードタイプ
    public Attribute CardAttribute;     // 属性
    public int basePower;               // 基本威力
    public Sprite icon;                 // アイコン画像
    public string description;          // 説明文
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///  Playerデータの定義
///  編集をしやすくするために置いているだけなのでここからデータ参照はしない
/// </summary>

[CreateAssetMenu(fileName = "PlayerEntity", menuName = "Create PlayerEntity")]
public class PlayerEntity : ScriptableObject
{
    public enum Attribute
    {
        Slash,      // 斬
        Blunt,      // 鈍
        Pierce,     // 突
        Bullet      // 弾
    }

    public int PlayerId;                // Player固有ID
    public string PlayerName;           // Player名
    public int PlayerHP;                // PlayerHp
    public int PlayerAttackPower;       // キャラ攻撃力
    public int PlayerDefensePower;      // キャラ防御力
    public Attribute PlayerAttribute;   // 属性
    public Sprite PlayerIcon;           // 立ち絵画像
    public Sprite PlayerSDIcon;         // SDキャラ
}
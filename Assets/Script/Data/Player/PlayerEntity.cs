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
    public int PlayerId;                          // Player固有ID
    public string PlayerName;                     // Player名
    public AttributeType PlayerAttribute;         // 属性
    public Sprite PlayerSprite;
    public string PlayerDescription;              // プレイヤーの説明文

    public int PlayerLevel;                       // PlayerLevel

    public float PlayerHP;                        // PlayerHp
    public float PlayerDefensePower;              // キャラ防御力
    public AnimatorSet AnimationSet;              // アニメーションセット
}
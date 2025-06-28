using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// <summary>
//  Playerデータの定義
//  編集をしやすくするために置いているだけなのでここからデータ参照はしない
// </summary>

[CreateAssetMenu(fileName = "PlayerEntity", menuName = "Create PlayerEntity")]
public class PlayerCharacterEntity : ScriptableObject
{
    public int PlayerID;                // キャラクター固有ID
    public string PlayerName;           // キャラクター名
    public int PlayerAttack;            // キャラクター攻撃力
    public int PlayerHP;                // チャラクターのHP
    public int basePower;               // 基本威力
    public Sprite SDChracter;           // SDキャラ画像
    public Sprite characterSprite;      // 立ち絵画像
}

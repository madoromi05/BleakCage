using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// <summary>
//  Playerデータの定義
//  編集をしやすくするために置いているだけなのでここからデータ参照はしない
// </summary>

[CreateAssetMenu(fileName = "PlayerEntity", menuName = "Create PlayerEntity")]
public class CardEntity : ScriptableObject
{
    public int PlayerID;                  // カード固有ID
    public string PlayerName;             // カード名
    public int basePower;               // 基本威力
    public Sprite icon;                 // アイコン画像
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// カードデータ本体
[CreateAssetMenu(fileName = "CardEntity", menuName = "Create CardEntity")]
public class CardEntity : ScriptableObject
{
    public new string name; // カード名
    public int hp;          // HP
    public int at;          // 攻撃力
    public int cost;        // コスト
    public Sprite icon;     // 画像（アイコン）
}
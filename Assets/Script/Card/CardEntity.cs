using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// カードデータ本体
[CreateAssetMenu(fileName = "CardEntity", menuName = "Create CardEntity")]
public class CardEntity : ScriptableObject
{
    public int CardID;
    public int WeaponAttack;    // 武器の攻撃力
    public Sprite icon;         // 画像（アイコン）
}
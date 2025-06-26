using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// カードデータ本体
[CreateAssetMenu(fileName = "CardEntity", menuName = "Create CardEntity")]
public class CardEntity : ScriptableObject
{
    public new string name;     // カード名
    public int WeaponAttack;    // HP
    public Sprite icon;         // 画像（アイコン）
}
using UnityEngine;

/// <summary>
///  Weaponデータの定義
///  編集をしやすくするために置いているだけなのでここからデータ参照はしない
/// </summary>

[CreateAssetMenu(fileName = "WeaponEntity", menuName = "Create WeaponEntity")]
public class WeaponEntity : ScriptableObject
{
    public float WeaponId;                          // Weapon固有ID
    public string WeaponName;                       // Weapon名
    public AttributeType WeaponAttribute;           // 属性
    public Sprite WeaponIcon;                       // 画像
    public string WeaponDescription;                // 説明文

    public float WeaponAttackPower;                 // 武器攻撃力
    public float WeaponPeakyCoefficient;            // 特化係数
}

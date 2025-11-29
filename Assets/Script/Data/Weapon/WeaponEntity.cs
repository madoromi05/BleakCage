using UnityEngine;

/// <summary>
///  Weaponデータの定義
///  編集をしやすくするために置いているだけなのでここからデータ参照はしない
/// </summary>

[CreateAssetMenu(fileName = "WeaponEntity", menuName = "Create WeaponEntity")]
public class WeaponEntity : ScriptableObject
{
    public float ID;                          // Weapon固有ID
    public string Name;                       // Weapon名
    public AttributeType Attribute;           // 属性
    public Sprite Icon;                       // 画像
    public string Description;                // 説明文

    public float AttackPower;                 // 武器攻撃力
    public float PeakyCoefficient;            // 特化係数
    public GameObject WeaponPrefab;
}

using UnityEngine;

/// <summary>
///  Weaponデータの定義
///  編集をしやすくするために置いているだけなのでここからデータ参照はしない
/// </summary>

[CreateAssetMenu(fileName = "WeaponEntity", menuName = "Create WeaponEntity")]
public class WeaponEntity : ScriptableObject
{
    public enum WeaponCategory
    {
        OneHandSword,       // 片手剣
        TwoHandSword,       // 両手剣
        KATANA,             // 刀
        Polearm,            // 長柄武器
        MartialArts,        // 拳術
        Shield,             // 盾
        Whip,               // 鞭
        Rapier,             // レイピア
        Mace,               // メイス
        Gun,                // 銃
        CompositeWeapon     // 複合武器
    }

    public enum Attribute
    {
        Slash,              // 斬
        Blunt,              // 鈍
        Pierce,             // 突
        Bullet              // 弾
    }

    public int WeaponID;                // Weapon固有ID
    public string WeaponName;           // Weapon名
    public int WeaponAttackPower;       // 武器攻撃力
    public Attribute WeaponAttribute;   // 属性
    public WeaponCategory weaponCategory;
    public int PeakyCoefficient;        // 特化係数
    public Sprite WeaponIcon;           // 画像
    public string WeaponDescription;    // 説明文
}

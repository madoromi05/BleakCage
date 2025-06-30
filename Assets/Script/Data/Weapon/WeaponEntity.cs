using UnityEngine;

/// <summary>
///  Weaponデータの定義
///  編集をしやすくするために置いているだけなのでここからデータ参照はしない
/// </summary>

[CreateAssetMenu(fileName = "WeaponEntity", menuName = "Create WeaponEntity")]
public class WeaponEntity : ScriptableObject
{
    public enum Attribute
    {
        Slash,      // 斬
        Blunt,      // 鈍
        Pierce,     // 突
        Bullet      // 弾
    }

    public int WeaponID;                // Weapon固有ID
    public string WeaponName;           // Weapon名
    public int WeaponAttack;            // 武器攻撃力
    public Attribute WeaponAttribute;   // 属性
    public int PeakyCoefficient;        // 特化係数
    public string description;          // 説明文
    public Sprite icon;                 // 画像
}

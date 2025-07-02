using UnityEngine;

/// <summary>
///  Enemyデータの定義
///  編集をしやすくするために置いているだけなのでここからデータ参照はしない
/// </summary>

[CreateAssetMenu(fileName = "EnemyEntity", menuName = "Create EnemyEntity")]
public class EnemyEntity : ScriptableObject
{
    public enum Attribute
    {
        Slash,      // 斬
        Blunt,      // 鈍
        Pierce,     // 突
        Bullet      // 弾
    }

    public int EnemyId;                 // Enemy固有ID
    public string EnemyName;            // Enemy名
    public float EnemyHP;               // EnemyのHP
    public float EnemyAttackPower;           // キャラ攻撃力
    public float EnemyDefensePower;          // キャラ防御力
    public Attribute EnemyAttribute;    // 属性
    public Sprite EnemyIcon;            // 戦闘中画像
}

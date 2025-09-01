using UnityEngine;

/// <summary>
///  Enemyデータの定義
///  編集をしやすくするために置いているだけなのでここからデータ参照はしない
/// </summary>

[CreateAssetMenu(fileName = "EnemyEntity", menuName = "Create EnemyEntity")]
public class EnemyEntity : ScriptableObject
{
    public int EnemyId;                              // Enemy固有ID
    public string EnemyName;                         // Enemy名
    public AttributeType EnemyAttribute;             // 攻撃属性
    public DefensAttributeType EnemyDefensAttribute; // 防御属性
    public Sprite EnemySprite;                       // 画像
    public string EnemyDescription;                  // 説明文

    public float EnemyHP;                            // EnemyのHP
    public float EnemyAttackPower;                   // キャラ攻撃力
    public float EnemyDefensePower;                  // キャラ防御力
}

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

    public int EnemyID;                // Enemy固有ID
    public string EnemyName;           // Enemy名
    public int CharacterAttack;         // キャラ攻撃力
    public int CharacterDefense;        // キャラ防御力
    public Attribute EnemyAttribute;   // 属性
    public Sprite icon;                 // 戦闘中画像
}

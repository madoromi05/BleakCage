using UnityEngine;

/// <summary>
///  Enemyデータの定義
///  編集をしやすくするために置いているだけなのでここからデータ参照はしない
/// </summary>

[CreateAssetMenu(fileName = "EnemyEntity", menuName = "Create EnemyEntity")]
public class EnemyEntity : ScriptableObject
{
    public enum AttackAttribute
    {
        Slash,      // 斬
        Blunt,      // 鈍
        Pierce,     // 突
        Bullet      // 弾
    }

    public int PlayerID;                // Enemy固有ID
    public string PlayerName;           // Enemy名
    public int CharacterAttack;         // キャラ攻撃力
    public int CharacterDefense;        // キャラ防御力
    public Sprite icon;                 // アイコン画像
}

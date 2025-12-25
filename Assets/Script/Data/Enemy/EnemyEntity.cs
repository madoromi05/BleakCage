using UnityEngine;

/// <summary>
///  Enemyデータの定義
///  編集をしやすくするために置いているだけなのでここからデータ参照はしない
/// </summary>

[CreateAssetMenu(fileName = "EnemyEntity", menuName = "Create EnemyEntity")]
public class EnemyEntity : ScriptableObject
{
    public int EnemyID;                     // Enemy固有ID
    public string EnemyName;                // Enemy名
    public AttributeType EnemyAttribute;    // 攻撃属性
    public DefensAttributeType EnemyDefensAttribute; // 防御属性
    public EnemyAttackType AttackType;      // 攻撃が近距離か遠距離か 
    public Sprite EnemySprite;              // 画像
    public string EnemyDescription;         // 説明文

    public float EnemyHP;                   // EnemyのHP
    public float EnemyAttackPower;          // キャラ攻撃力
    public float EnemyDefensePower;         // キャラ防御
    public float EnemyAttackOffset;         // Enemy1の攻撃アニメーション位置がおかしいため追加

    public GameObject CharacterPrefab;      // キャラクタープレハブ
    public Vector3 InitialRotation;         // 初期回転
    public GameObject AttackEffectPrefab;   // 攻撃エフェクトプレハブ
}
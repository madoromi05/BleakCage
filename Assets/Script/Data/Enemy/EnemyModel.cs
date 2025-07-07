using UnityEngine;

/// <summary>
/// 実行時に使用される敵キャラクターのモデルクラス
/// </summary>
public class EnemyModel
{
    public int EnemyId { get; private set; }                          // 敵のID
    public string EnemyName { get; private set; }                     // 敵の名前
    public float EnemyHP { get; private set; }                        // 敵のHP
    public float EnemyAttackPower { get; private set; }              // 攻撃力
    public float EnemyDefensePower { get; private set; }             // 防御力
    public AttackAttributeType EnemyAttribute { get; private set; }  // 属性
    public Sprite EnemyIcon { get; private set; }                    // 表示アイコン
    public string EnemyDescription { get; private set; }             // 説明文

    /// <summary>
    /// ScriptableObject(EnemyEntity)からデータを読み込んでモデルに反映
    /// </summary>
    /// <param name="enemyId">敵のID</param>
    public EnemyModel(EnemyEntity enemyEntity)
    {
        //初期化
        if (enemyEntity == null)
        {
            Debug.LogError("enemyEntity is null");
            return;
        }
        EnemyId = enemyEntity.EnemyId;
        EnemyName = enemyEntity.EnemyName;
        EnemyHP = enemyEntity.EnemyHP;
        EnemyAttackPower = enemyEntity.EnemyAttackPower;
        EnemyDefensePower = enemyEntity.EnemyDefensePower;
        EnemyAttribute = enemyEntity.EnemyAttribute;
        EnemyIcon = enemyEntity.EnemyIcon;
        EnemyDescription = enemyEntity.EnemyDescription;
    }
}

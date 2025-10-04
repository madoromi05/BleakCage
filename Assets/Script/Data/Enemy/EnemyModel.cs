using UnityEngine;

/// <summary>
/// 実行時に使用される敵キャラクターのモデルクラス
/// </summary>
public class EnemyModel
{
    public int EnemyId { get; private set; }                                // 敵のID
    public string EnemyName { get; private set; }                           // 敵の名前
    public float EnemyHP { get; set; }                                      // 敵のHP
    public float EnemyAttackPower { get; private set; }                     // 攻撃力
    public float EnemyDefensePower { get; private set; }                    // 防御力
    public AttributeType EnemyAttribute { get; private set; }               // 攻撃属性
    public DefensAttributeType EnemyDefensAttribute { get; private set; }   // 防御属性
    public Sprite EnemySprite { get; private set; }                           // 表示アイコン
    public string EnemyDescription { get; private set; }                    // 説明文

    public EnemyDrop  EnemyDrop { get; private set; }

    /// <summary>
    /// ScriptableObject(EnemyEntity)からデータを読み込んでモデルに反映
    /// </summary>
    /// <param name="enemyId">敵のID</param>
    public EnemyModel(EnemyEntity Entity)
    {
        //初期化
        if (Entity == null)
        {
            Debug.LogError("enemyEntity is null");
            return;
        }
        EnemyId = Entity.EnemyId;
        EnemyName = Entity.EnemyName;
        EnemyHP = Entity.EnemyHP;
        EnemyAttackPower = Entity.EnemyAttackPower;
        EnemyDefensePower = Entity.EnemyDefensePower;
        EnemyAttribute = Entity.EnemyAttribute;
        EnemyDefensAttribute = Entity.EnemyDefensAttribute;
        EnemySprite = Entity.EnemySprite;
        EnemyDescription = Entity.EnemyDescription;
    }
}

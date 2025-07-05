using UnityEngine;

public class EnemyModel
{
    public int EnemyId { get; set; }
    public string EnemyName { get; set; }
    public float EnemyHP { get; set; }                          // EnemyのHP
    public float EnemyAttackPower { get; set; }                 // キャラ攻撃力
    public float EnemyDefensePower { get; set; }                // キャラ防御力
    public EnemyEntity.Attribute EnemyAttribute { get; set; }   // 属性
    public Sprite EnemyIcon { get; set; }                       // 戦闘中画像
    public string EnemyDescription { get; set; }                //説明文

    // コンストラクタ（敵IDを引数にしてデータを読み込む）
    public EnemyModel(int enemyId)
    {
        // Resourcesフォルダから敵データを取得
        EnemyEntity enemyEntity = Resources.Load<EnemyEntity>("EnemyEntityList/Enemy" + enemyId);

        if (enemyEntity == null)
        {
            Debug.LogError($"EnemyEntity not found for ID: {enemyId}");
            return;
        }

        // 取得したデータをEnemyModelに反映
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

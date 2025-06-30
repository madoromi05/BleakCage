using UnityEngine;

public class EnemyModel
{
    public int EnemyId { get; set; }
    public string EnemyName { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public EnemyEntity.Attribute EnemyAttribute { get; set; }
    public Sprite Icon { get; set; }

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
        EnemyId = enemyEntity.EnemyID;
        EnemyName = enemyEntity.EnemyName;
        Attack = enemyEntity.CharacterAttack;
        Defense = enemyEntity.CharacterDefense;
        EnemyAttribute = enemyEntity.EnemyAttribute;
        Icon = enemyEntity.icon;
    }
}
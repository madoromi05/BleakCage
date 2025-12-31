using UnityEngine;

/// <summary>
/// EnemyModelを生成するファクトリクラス
/// </summary>
public class EnemyModelFactory
{
    /// <summary>
    /// IDからEnemyModelを生成
    /// </summary>
    public EnemyModel CreateFromId(int enemyId)
    {
        EnemyEntity enemyEntity = LoadEnemyEntity(enemyId);
        if (enemyEntity == null)
        {
            DebugCostom.LogError($"EnemyEntity not found for ID: {enemyId}");
            return null;
        }
        return new EnemyModel(enemyEntity);
    }

    /// <summary>
    /// EnemyEntityを読み込む
    /// </summary>
    private EnemyEntity LoadEnemyEntity(int enemyId)
    {
        string path = $"EntityDataList/EnemyEntityList/Enemy_{enemyId}";
        EnemyEntity enemyEntity = Resources.Load<EnemyEntity>(path);

        if (enemyEntity == null)
        {
            DebugCostom.LogWarning($"EnemyEntity not found at path: {path}");
        }

        return enemyEntity;
    }
}
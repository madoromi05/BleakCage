using UnityEngine;

/// <summary>
/// EnemyModelを生成するファクトリクラス
/// 責任：EnemyEntityの読み込みとEnemyModelの生成
/// </summary>
public class EnemyModelFactory
{
    /// <summary>
    /// IDからEnemyModelを生成
    /// </summary>
    /// <param name="enemyId">敵ID</param>
    /// <returns>EnemyModel。生成に失敗した場合はnull</returns>
    public EnemyModel CreateFromId(int enemyId)
    {
        EnemyEntity enemyEntity = LoadEnemyEntity(enemyId);
        if (enemyEntity == null)
        {
            Debug.LogError($"EnemyEntity not found for ID: {enemyId}");
            return null;
        }
        return new EnemyModel(enemyEntity);
    }

    /// <summary>
    /// EnemyEntityから直接EnemyModelを生成
    /// </summary>
    /// <param name="enemyEntity">EnemyEntity</param>
    /// <returns>EnemyModel。生成に失敗した場合はnull</returns>
    public EnemyModel CreateFromEntity(EnemyEntity enemyEntity)
    {
        if (enemyEntity == null)
        {
            Debug.LogError("EnemyEntity is null");
            return null;
        }
        return new EnemyModel(enemyEntity);
    }

    /// <summary>
    /// 複数のEnemyModelを一括生成
    /// </summary>
    /// <param name="enemyIds">敵IDの配列</param>
    /// <returns>EnemyModelの配列（失敗したものはnull）</returns>
    public EnemyModel[] CreateMultipleFromIds(int[] enemyIds)
    {
        if (enemyIds == null || enemyIds.Length == 0)
        {
            Debug.LogWarning("EnemyIds array is null or empty");
            return new EnemyModel[0];
        }

        EnemyModel[] enemyModels = new EnemyModel[enemyIds.Length];
        for (int i = 0; i < enemyIds.Length; i++)
        {
            enemyModels[i] = CreateFromId(enemyIds[i]);
        }
        return enemyModels;
    }

    /// <summary>
    /// EnemyEntityを読み込む
    /// </summary>
    /// <param name="enemyId">敵ID</param>
    /// <returns>EnemyEntity。見つからない場合はnull</returns>
    private EnemyEntity LoadEnemyEntity(int enemyId)
    {
        string path = $"EnemyEntityList/Enemy_{enemyId}";
        EnemyEntity enemyEntity = Resources.Load<EnemyEntity>(path);

        if (enemyEntity == null)
        {
            Debug.LogWarning($"EnemyEntity not found at path: {path}");
        }

        return enemyEntity;
    }
}
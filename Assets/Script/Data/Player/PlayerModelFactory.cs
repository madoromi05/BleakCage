using UnityEngine;

/// <summary>
/// PlayerModelを生成するファクトリクラス
/// 責任：PlayerEntityの読み込みとPlayerModelの生成
/// </summary>
public class PlayerModelFactory
{
    /// <summary>
    /// IDからPlayerModelを生成
    /// </summary>
    /// <param name="playerId">プレイヤーID</param>
    /// <returns>PlayerModel。生成に失敗した場合はnull</returns>
    public PlayerModel CreateFromId(int playerId)
    {
        PlayerEntity playerEntity = LoadPlayerEntity(playerId);
        if (playerEntity == null)
        {
            Debug.LogError($"PlayerEntity not found for ID: {playerId}");
            return null;
        }
        return new PlayerModel(playerEntity);
    }

    /// <summary>
    /// PlayerEntityから直接PlayerModelを生成
    /// </summary>
    /// <param name="playerEntity">PlayerEntity</param>
    /// <returns>PlayerModel。生成に失敗した場合はnull</returns>
    public PlayerModel CreateFromEntity(PlayerEntity playerEntity)
    {
        if (playerEntity == null)
        {
            Debug.LogError("PlayerEntity is null");
            return null;
        }
        return new PlayerModel(playerEntity);
    }

    /// <summary>
    /// 複数のPlayerModelを一括生成
    /// </summary>
    /// <param name="playerIds">プレイヤーIDの配列</param>
    /// <returns>PlayerModelの配列（失敗したものはnull）</returns>
    public PlayerModel[] CreateMultipleFromIds(int[] playerIds)
    {
        if (playerIds == null || playerIds.Length == 0)
        {
            Debug.LogWarning("PlayerIds array is null or empty");
            return new PlayerModel[0];
        }

        PlayerModel[] playerModels = new PlayerModel[playerIds.Length];
        for (int i = 0; i < playerIds.Length; i++)
        {
            playerModels[i] = CreateFromId(playerIds[i]);
        }
        return playerModels;
    }

    /// <summary>
    /// PlayerEntityを読み込む
    /// </summary>
    /// <param name="playerId">プレイヤーID</param>
    /// <returns>PlayerEntity。見つからない場合はnull</returns>
    private PlayerEntity LoadPlayerEntity(int playerId)
    {
        string path = $"PlayerEntityList/Player_{playerId}";
        PlayerEntity playerEntity = Resources.Load<PlayerEntity>(path);

        if (playerEntity == null)
        {
            Debug.LogWarning($"PlayerEntity not found at path: {path}");
        }

        return playerEntity;
    }
}

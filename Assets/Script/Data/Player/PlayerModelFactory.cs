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

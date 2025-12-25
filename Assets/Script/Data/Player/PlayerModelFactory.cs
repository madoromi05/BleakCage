using System.Linq;
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
        string folderPath = "EntityDataList/PlayerEntityList";
        PlayerEntity[] allEntities = Resources.LoadAll<PlayerEntity>(folderPath);
        string searchPrefix = $"Player_{playerId}";

        PlayerEntity targetEntity = allEntities
            .FirstOrDefault(e => e.name.StartsWith(searchPrefix));

        if (targetEntity == null)
        {
            Debug.LogError($"ID: {playerId} (検索名: {searchPrefix}...) に一致するファイルが見つかりません。");
            return null;
        }

        return new PlayerModel(targetEntity);
    }
}

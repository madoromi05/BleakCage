using System.Linq;
using UnityEngine;

/// <summary>
/// ID からPlayerModelを生成するファクトリクラス
/// PlayerEntityの読み込みとPlayerModelの生成
/// </summary>
public class PlayerModelFactory
{
    public PlayerModel CreateFromId(int playerId)
    {
        string folderPath = "EntityDataList/PlayerEntityList";
        PlayerEntity[] allEntities = Resources.LoadAll<PlayerEntity>(folderPath);
        string searchPrefix = $"Player_{playerId}";

        PlayerEntity targetEntity = allEntities
            .FirstOrDefault(e => e.name.StartsWith(searchPrefix));

        if (targetEntity == null)
        {
            DebugCostom.LogError($"ID: {playerId} (検索名: {searchPrefix}...) に一致するファイルが見つかりません。");
            return null;
        }

        return new PlayerModel(targetEntity);
    }
}

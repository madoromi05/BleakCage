using UnityEngine;

/// <summary>
/// CardModelを生成するファクトリクラス
/// 責任：CardEntityの読み込みとCardModelの生成
/// </summary>
public class CardModelFactory
{
    /// <summary>
    /// IDからCardModelを生成
    /// </summary>
    /// <param name="cardId">カードID</param>
    /// <returns>CardModel。生成に失敗した場合はnull</returns>
    public CardModel CreateFromID(int cardId)
    {
        CardEntity cardEntity = LoadCardEntity(cardId);
        if (cardEntity == null)
        {
            Debug.LogError($"CardEntity not found for ID: {cardId}");
            return null;
        }
        return new CardModel(cardEntity);
    }

    /// <summary>
    /// CardEntityを読み込む
    /// </summary>
    /// <param name="cardId">カードID</param>
    /// <returns>CardEntity。見つからない場合はnull</returns>
    private CardEntity LoadCardEntity(int cardId)
    {
        string path = $"EntityDataList/CardEntityList/Card_{cardId}";
        CardEntity cardEntity = Resources.Load<CardEntity>(path);

        if (cardEntity == null)
        {
            Debug.LogWarning($"CardEntity not found at path: {path}");
        }

        return cardEntity;
    }
}
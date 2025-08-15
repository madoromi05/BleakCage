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
    /// CardEntityから直接CardModelを生成
    /// </summary>
    /// <param name="cardEntity">CardEntity</param>
    /// <returns>CardModel。生成に失敗した場合はnull</returns>
    public CardModel CreateFromEntity(CardEntity cardEntity)
    {
        if (cardEntity == null)
        {
            Debug.LogError("CardEntity is null");
            return null;
        }
        return new CardModel(cardEntity);
    }

    /// <summary>
    /// 複数のCardModelを一括生成
    /// いらないかも
    /// </summary>
    /// <param name="cardIds">カードIDの配列</param>
    /// <returns>CardModelの配列（失敗したものはnull）</returns>
    public CardModel[] CreateMultipleFromIds(int[] cardIds)
    {
        if (cardIds == null || cardIds.Length == 0)
        {
            Debug.LogWarning("CardIds array is null or empty");
            return new CardModel[0];
        }

        CardModel[] cardModels = new CardModel[cardIds.Length];
        for (int i = 0; i < cardIds.Length; i++)
        {
            cardModels[i] = CreateFromID(cardIds[i]);
        }
        return cardModels;
    }

    /// <summary>
    /// CardEntityを読み込む
    /// </summary>
    /// <param name="cardId">カードID</param>
    /// <returns>CardEntity。見つからない場合はnull</returns>
    private CardEntity LoadCardEntity(int cardId)
    {
        string path = $"CardEntityList/Card_{cardId}";
        CardEntity cardEntity = Resources.Load<CardEntity>(path);

        if (cardEntity == null)
        {
            Debug.LogWarning($"CardEntity not found at path: {path}");
        }

        return cardEntity;
    }
}
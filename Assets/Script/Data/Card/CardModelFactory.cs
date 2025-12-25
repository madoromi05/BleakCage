using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CardModelFactory
{
    private static Dictionary<int, CardEntity> cardCache;

    private const string BaseLoadPath = "EntityDataList/CardEntityList";

    public CardModelFactory()
    {
        // まだ辞書を作っていなければ、作る処理を実行
        if (cardCache == null)
        {
            LoadAllCardEntities();
        }
    }

    // JSONのデータ(ID)を使って、本物のデータ(Model)を作る
    public CardModel CreateFromID(int cardId)
    {
        if (cardCache.TryGetValue(cardId, out CardEntity entity))
        {
            // 見つかったらモデルを作って返す
            return new CardModel(entity);
        }

        Debug.LogError($"ID: {cardId} のカードが見つかりません。Resources/{BaseLoadPath} 以下のどこかにファイルが存在するか、IDが正しいか確認してください。");
        return null;
    }

    // 全ファイルを読み込んで辞書を作る処理
    private void LoadAllCardEntities()
    {
        cardCache = new Dictionary<int, CardEntity>();

        CardEntity[] allCards = Resources.LoadAll<CardEntity>(BaseLoadPath);

        if (allCards.Length == 0)
        {
            Debug.LogWarning($"パス: {BaseLoadPath} にカードデータが一つも見つかりません！パスが合っているか確認してください。");
            return;
        }

        foreach (var card in allCards)
        {
            // IDが被っていないかチェックしながら登録
            if (card != null && !cardCache.ContainsKey(card.ID))
            {
                cardCache.Add(card.ID, card);
            }
            else if (card != null)
            {
                Debug.LogWarning($"ID重複エラー: ID {card.ID} が複数のファイルで使われています: {card.name}");
            }
        }
    }
}
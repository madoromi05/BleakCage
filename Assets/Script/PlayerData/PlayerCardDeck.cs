using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.GPUSort;

/// <summary>
/// Playerのデッキ(編集し終わった)を管理するクラス
/// </summary>
public class PlayerCardDeck : MonoBehaviour
{
    public List<int> decklist { get; protected set; } = new List<int>();                            // 実際に使用するデッキのIDリスト
    public List<CardEntity> cards { get; protected set; } = new List<CardEntity>();                 // カードのデータを保持するリスト
    public Dictionary<int, int> dicdecklist { get; protected set; } = new Dictionary<int, int>();   // デッキのIDをキーとした辞書。カードIDをキー、インデックスを値とする。

    private int cardIdentifier;                         //処理中のカードID
    private int cardcount = 0;                          // 現在のデッキに入っているカードの数
    private int decksheet = 42;                         // デッキの最大枚数

    void Start()
    {
        CreateDeck();
    }

    private void CreateDeck()
    {
        decklist.Clear();
        cards.Clear();
        dicdecklist.Clear();
        cardcount = 0;

        for (int i = 0; i < decksheet; i++)
        {
            decklist.Add(-1);
        }

        for (int i = 1; i <= decksheet; i++)
        {
            CardEntity entity = Resources.Load<CardEntity>($"CardEntityList/Card_{i}");
            if (entity != null)
            {
                cards.Add(entity);
                DeckKeep(entity);
            }
            else
            {
                Debug.LogWarning($"CardEntity not found: Card_{i}");
            }
        }
    }

    // IDを指定してCardEntityをデッキに追加する
    private void DeckKeep(CardEntity cardEntity)
    {
        if (cardcount < decksheet)
        {
            cardIdentifier = cardEntity.CardIdentifier;
            decklist.Add(cardIdentifier);
            dicdecklist[cardIdentifier] = cardcount;
            cardcount++;
        }
    }
}
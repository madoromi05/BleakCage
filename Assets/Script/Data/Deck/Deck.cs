using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// カードを初期デッキとして保持する
/// </summary>
public class Deck : MonoBehaviour
{
    [SerializeField] private GameObject characters;
    [SerializeField] private GameObject weapons;

    public List<int> decklist;                          // 実際に使用するデッキのIDリスト
    public List<CardEntity> cards;                      // カードのデータを保持するリスト
    public Dictionary<int, int> dicdecklist;            // デッキのIDをキーとした辞書。カードIDをキー、インデックスを値とする。
    public List<int> drawPile = new();

    private int CardID;                                 //処理中のカードID
    private int cardcount = 0;                          // 現在のデッキに入っているカードの数
    private int decksheet = 42;                         // デッキの最大枚数

        void Start()
    {
        decklist = new List<int>();
        cards = new List<CardEntity>();
        dicdecklist = new Dictionary<int, int>();

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
                drawPile.Add(CardID);
            }
            else
            {
                Debug.LogWarning($"CardEntity not found: Card_{i}");
            }
        }
    }

    private void DeckKeep(CardEntity cardEntity)
    {
        if (cardcount < decksheet)
        {
            CardID = cardEntity.CardId;
            decklist[cardcount] = CardID;
            dicdecklist[cardcount] = CardID;
            cardcount++;
        }
    }

    public bool TryDrawCard(List<int> excludeIds, out int cardId)
    {
        var candidates = drawPile.Where(id => !excludeIds.Contains(id)).ToList();
        if (candidates.Count == 0)
        {
            cardId = -1;
            Debug.LogWarning("除外リストによりカードが引けません");
            return false;
        }

        int randIndex = Random.Range(0, candidates.Count);
        cardId = candidates[randIndex];
        return true;
    }

    //編集するときに管理したい感はある
    //void SearchCard()
    //{
    //    //武器やキャラをWeaponTagやCharacterTagで見つける

    //    if(weapons != null)
    //    {
    //        for(int i = 0; i < 3; i++)
    //        {
    //            DeckKeep(objecter[i]);
    //        }
    //        objecter.Clear();
    //    }
    //    if(characters != null)
    //    {
    //        for (int i = 0; i < 3; i++)
    //        {
    //            DeckKeep(objecter[i]);
    //        }
    //        objecter.Clear();
    //    }
    //}
}

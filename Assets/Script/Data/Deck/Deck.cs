using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// カードを初期デッキとして保持する
/// </summary>
public class Deck : MonoBehaviour
{
    [SerializeField] private GameObject characters;
    [SerializeField] private GameObject weapons;

    public List<int> decklist;                          //実際に使用するデッキのIDリスト
    public List<int> objecter;                          //カードのインデックスなどを一時的に格納するリスト。
    public List<CardEntity> cards;                      // カードのデータを保持するリスト
    public Dictionary<int, int> dicdecklist;            // デッキのIDをキーとした辞書。カードIDをキー、インデックスを値とする。

    private int CardID;                                 //処理中のカードID
    private int cardcount = 0;                          // 現在のデッキに入っているカードの数
    private int decksheet = 42;                         // デッキの最大枚数

    void Start()
    {
        //一旦初期化
        decklist = new List<int>();
        objecter = new List<int>();
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
            //カードをデッキに入れる
            CardID = cardEntity.CardId;
            decklist[cardcount] = CardID;
            dicdecklist[cardcount] = CardID;
            cardcount++;
        }
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

using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// カードを初期デッキとして保持する
/// </summary>
public class Deck : MonoBehaviour
{
    public List<int> decklist;
    public List<int> objecter;
    public List<CardEntity> cards;
    public Dictionary<int,int> dicdecklist;
    [SerializeField] private GameObject characters;
    [SerializeField] private GameObject weapons;
    int CardID;
    int cardcount = 0;
    int decksheet = 42;

    void DeckKeep(int i)
    {
        if(cardcount < decksheet)
        {
            //カードをデッキに入れる
            CardID = cards[i].cardID;
            decklist[cardcount] = CardID;
            dicdecklist[cardcount] = CardID;
            cardcount++;
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //一旦初期化
        decklist = new List<int>();
        objecter = new List<int>();
        cards = new List<CardEntity>();
        dicdecklist = new Dictionary<int,int>();

        for(int i = 0; i < decksheet; i++)
        {
            decklist.Add(-1);
        }
    }

    void SearchCard()
    {
        //武器やキャラをWeaponTagやCharacterTagで見つける

        if(weapons != null)
        {
            for(int i = 0; i < 3; i++)
            {
                DeckKeep(objecter[i]);
            }
            objecter.Clear();
        }
        if(characters != null)
        {
            for (int i = 0; i < 3; i++)
            {
                DeckKeep(objecter[i]);
            }
            objecter.Clear();
        }
    }

    //起動スイッチはキャラとかつくってからで
}

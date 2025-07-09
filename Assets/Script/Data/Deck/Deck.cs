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

    GameObject weapons;
    GameObject characters;
    int CardID;
    int cardcount = 0;

    void DeckKeep(int i)
    {
        if(cardcount < 42)
        {
            //カードをデッキに入れる
            CardID = cards[i].CardId;
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

        for(int i = 0; i < 42; i++)
        {
            decklist.Add(-1);
        }
    }

    void SearchCard()
    {
        //武器やキャラをWeaponTagやCharacterTagで見つける
        weapons = GameObject.FindGameObjectWithTag("Weapon");
        characters = GameObject.FindGameObjectWithTag("Character");

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

    void Update()
    {

    }
}

using UnityEngine;
using System.Collections;
using Unity.VisualScripting; // IEnumerator を使うために必要
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    bool isPlayerTurn;
    [SerializeField] CardController cardPrefab;
    [SerializeField] Transform PlayerHandTransform;
    [SerializeField] Dictionary<int, int> deck;

    List<int> handcard;
    List<CardController> card;
    public int deckcardid;
    public Transform hand;
    public GameObject check;
    bool hand1, hand2, hand3;
    int limitcard = 0;

    void Start()
    {
        StartGame();
    }

    void StartGame()
    {
        isPlayerTurn = true;
        TurnCalc();
        CreateCard(PlayerHandTransform);
    }

    void TurnCalc()
    {
        if (isPlayerTurn)
        {
            StartCoroutine(PlayerTurnCoroutine()); // コルーチンでプレイヤーターンを管理
        }
        else
        {
            EnemyTurn();
        }
    }

    IEnumerator PlayerTurnCoroutine()
    {
        Debug.Log("プレイヤーのターン開始");
        CreateCard(hand);
        // 10秒待機
        yield return new WaitForSeconds(10f);

        Debug.Log("10秒経過したのでターンを終了します");
        ChangeTurn(); // 敵のターンに切り替え
    }

    void EnemyTurn()
    {
        Debug.Log("敵のターンです");
        ChangeTurn(); // すぐにプレイヤーターンに戻す（必要に応じて修正）
    }

    public void ChangeTurn()
    {
        isPlayerTurn = !isPlayerTurn;
        if (isPlayerTurn)
        {
            // プレイヤーのターン開始時処理
        }
        else
        {
            // 敵のターン開始時処理
        }
        // 次のターンの処理を開始
        TurnCalc();
    }

    IEnumerator CreateCard(Transform hand)
    {
        //三枚提示
        for(int i = 0; i < 3; i++)
        {
            int draw = Random.Range(0, 41);
            card[i] = Instantiate(cardPrefab, hand, false);
            deck.Remove(draw);
            handcard[i] = draw;
        }

        //選択し、選択されたものにチェックを表示する
        if(Input.GetKey(KeyCode.Alpha1))
        {
            if (limitcard < 2)
            {
                hand1 = !hand1;
                Instantiate(check, hand, hand1);
            }
            hand1 = !hand1;
            if(hand1)
            {
                limitcard += 1;
            }
            else 
            {
                limitcard -= 1;
            }
                Instantiate(check, hand, hand1);
        }
        else if(Input.GetKey(KeyCode.Alpha2))
        {
            if(limitcard < 2)
            {
                hand2 = !hand2;
                Instantiate(check, hand, hand2);
            }

            if (hand2)
            {
                limitcard += 1;
            }
            else
            {
                limitcard -= 1;
            }
        }
        else if(Input.GetKey(KeyCode.Alpha3))
        {
            if (limitcard < 2)
            {
                hand3 = !hand3;
                Instantiate(check, hand, hand3);
            }

            if (hand3)
            {
                limitcard += 1;
            }
            else
            {
                limitcard -= 1;
            }
        }
        yield return new WaitForSeconds(10f);

        yield break ;
    }
}
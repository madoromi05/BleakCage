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
    public int deckcardid;
    public Transform hand;

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

    void CreateCard(Transform hand)
    {
        for(int i = 0; i < 3; i++)
        {
            int draw = Random.Range(0, 41);
            CardController card = Instantiate(cardPrefab, hand, false);
            card.Init(draw);
            deck.Remove(i);
        }
        if(Input.GetKey(KeyCode.Alpha1))
        {

        }
        else if(Input.GetKey(KeyCode.Alpha2))
        {

        }
        else if(Input.GetKey(KeyCode.Alpha3))
        {

        }

    }
}
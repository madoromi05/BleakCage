using UnityEngine;
using System.Collections; // IEnumerator を使うために必要

public class BattleManager : MonoBehaviour
{
    bool isPlayerTurn;
    [SerializeField] CardController cardPrefab;
    [SerializeField] Transform PlayerHandTransform;

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
        CardController card = Instantiate(cardPrefab, hand, false);
        card.Init(1);
    }
}
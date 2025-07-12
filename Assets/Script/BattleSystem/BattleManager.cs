using UnityEngine;
using System.Collections;
using Unity.VisualScripting; // IEnumerator を使うために必要
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    bool isPlayerTurn;
    [SerializeField] CardController cardPrefab;
    [SerializeField] Transform PlayerHandTransform;
    List<CardController> card;
    public Transform hand;

    //デッキ
    [SerializeField] Dictionary<int, int> deck;
    //何回目に何が破棄されたか
    private Dictionary<int, int> trush;
    //0から41のリスト
    private List<int> random;
    //手札のカードID
    List<int> handcard;
    //選択されたカードにわかるようにチェックを出す
    public GameObject check;
    //手札の１，２，３の判定
    bool hand1, hand2, hand3;
    //手札の１，２，３が選ばれたか
    int hand1count, hand2count, hand3count;
    //2枚の選択上限
    int limitcard = 0;
    //全ターンを通して破棄された回数
    int trushcount = 0;
    //１ターンを通して破棄された回数
    int trushturncount = 0;
    //デッキ枚数
    int decksheet = 42;
    //選択されたカード枚数
    int selectcard = 0; 

    void Start()
    {
        card = new List<CardController>();
        handcard = new List<int>();
        trush = new Dictionary<int, int>();
        random = new List<int>();
        for (int i = 0; i < decksheet; i++)
        {
            random.Add(i);
        }
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
        Cardcreate();
        float timer = 0f;
        while (timer < 10)
        {
            //選択し、選択されたものにチェックを表示する
           if(Input.GetKeyDown(KeyCode.Alpha1))
            {
                //一枚目選択
                if (limitcard < 2)
                {
                    hand1 = !hand1;
                    Instantiate(check, hand, hand1);
                }
                hand1 = !hand1;
                if(hand1)
                {
                    limitcard += 1;
                    selectcard += 1;
                    hand1count = 1;
                }
                else 
                {
                    limitcard -= 1;
                    selectcard -= 1;
                    hand1count = 0;
                }
            }
            else if(Input.GetKeyDown(KeyCode.Alpha2))
            {
                //二枚目選択
                if(limitcard < 2)
                {
                   hand2 = !hand2;
                    Instantiate(check, hand, hand2);
                }

                if (hand2)
                {
                    limitcard += 1;
                    selectcard += 1;
                   hand2count = 1;
                }
                else
                {
                    limitcard -= 1;
                    selectcard -= 1;
                    hand2count = 0;
                }
            }
            else if(Input.GetKeyDown(KeyCode.Alpha3))
            {
                //三枚目選択
                if (limitcard < 2)
                {
                   hand3 = !hand3;
                   Instantiate(check, hand, hand3);
                }

                if (hand3)
                {
                    limitcard += 1;
                    selectcard += 1;
                    hand3count = 1;
                }
                else
                {
                    limitcard -= 1;
                    selectcard -= 1;
                    hand3count = 0;
                }
            }

            if (Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                //破棄と再製
                if (selectcard < 3 && selectcard > 0 || trushturncount < 3)
                {
                    if (hand1count == 1)
                    {
                        trush[trushcount] = handcard[0];
                        trushcount++;
                    }
                    if (hand2count == 1)
                    {

                        trush[trushcount] = handcard[1];
                        trushcount++;
                    }
                    if (hand3count == 1)
                    {

                        trush[trushcount] = handcard[2];
                        trushcount++;
                    }
                    trushturncount++;
                    Cardcreate();
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }

        //ここでのことを全て初期化
        trushturncount = 0;
        hand1count = 0;
        hand2count = 0;
        hand3count = 0;
        limitcard = 0;

        handcard.Clear();

        yield break ;
    }
    void Cardcreate()
    {

        //三枚提示
        for (int i = 0; i < 3; i++)
        {
            int draw = Random.Range(0, 41);
            var c = Instantiate(cardPrefab, hand, false);
            card.Add(c);
            deck.Remove(draw);
            handcard.Add(draw);
        }
    }
}
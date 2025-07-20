using UnityEngine;
using System.Collections;
using Unity.VisualScripting; // IEnumerator を使うために必要
using System.Collections.Generic;
using System.Linq;

public class BattleManager : MonoBehaviour
{
    [SerializeField] CardController cardPrefab;
    [SerializeField] Transform PlayerHandTransform;
    [SerializeField] private GameObject checkEffect;    // 選択されたカードにエフェクト付与用

    List<CardController> card;
    private CardModelFactory cardModelFactory;          // カードモデル生成用

    private List<int> selectedCardsThisTurn = new List<int>();  // 選択されたカードのIDを保持
    private List<int> excludedCardsThisTurn = new List<int>();　// 破棄されたカードのIDを保持
    List<int> handcard;                                         // 手札のカードID
    private bool isPlayerTurn;                                  // プレイヤーのターンかどうか
    private float turnTime = 10f;                               // ターンの制限時間（秒）
    private int decksheet = 42;                                 // デッキの最大枚数

    // カード選択状態管理
    private bool[] handSelected = new bool[3];                   // 各カード（3枚）が選択されているかどうか
    private GameObject[] selectionEffects = new GameObject[3];   // 各カードの選択エフェクト

    void Start()
    {
        card = new List<CardController>();
        handcard = new List<int>();
        cardModelFactory = new CardModelFactory();
        StartGame();
    }

    private void StartGame()
    {
        isPlayerTurn = true;
        TurnCalc();                         // Playerのターンを開始
    }

    private void TurnCalc()
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

    private IEnumerator PlayerTurnCoroutine()
    {
        Debug.Log("プレイヤーのターン開始");
        // カードを生成して表示
        yield return StartCoroutine(CreateCard(PlayerHandTransform));

        Debug.Log("10秒経過したのでターンを終了します");
        ChangeTurn(); // 敵のターンに切り替え
    }

    private void EnemyTurn()
    {
        Debug.Log("敵のターンです");
        ChangeTurn(); // すぐにプレイヤーターンに戻す（必要に応じて修正）
    }

    private void ChangeTurn()
    {
        isPlayerTurn = !isPlayerTurn;

        selectedCardsThisTurn.Clear();
        excludedCardsThisTurn.Clear();
        for (int i = 0; i < handSelected.Length; i++)
        {
            handSelected[i] = false;
        }

        // 選択エフェクトをリセット
        ResetSelectionEffects();

        TurnCalc();
    }

    /// <summary>
    /// 待機しているときだけ入力受付
    /// </summary>
    /// <param name="hand"></param>
    /// <returns></returns>
    private IEnumerator CreateCard(Transform hand)
    {
        Cardcreate();
        float timer = 0f;

        //カード選択メゾット
        while (timer < turnTime)
        {
            //選択し、選択されたものにチェックを表示する
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                CardSelect(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                CardSelect(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                CardSelect(2);
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                for (int i = 0; i < handSelected.Length; i++)
                {
                    if (handSelected[i])
                    {
                        selectedCardsThisTurn.Add(handcard[i]); // 選ばれたカードを追加
                    }
                    else
                    {
                        excludedCardsThisTurn.Add(handcard[i]); // 選ばれなかったカードを一時除外
                    }
                }

                Debug.Log("選択カード: " + string.Join(",", selectedCardsThisTurn));
                Debug.Log("除外カード: " + string.Join(",", excludedCardsThisTurn));

                //選択状態をリセット
                ResetSelectionEffects();
                for (int i = 0; i < handSelected.Length; i++)
                {
                    handSelected[i] = false;
                }

                Cardcreate();
            }
            timer += Time.deltaTime;
            yield return null;
        }
        handcard.Clear();

        yield break;
    }

    private void Cardcreate()
    {
        // 既存のカードを消す
        foreach (Transform child in PlayerHandTransform)
        {
            Destroy(child.gameObject);
        }

        card.Clear();
        handcard.Clear();
        ResetSelectionEffects();

        //三枚提示
        for (int i = 0; i < 3; i++)
        {
            int draw;
            do
            {
                draw = Random.Range(1, decksheet + 1); // CardEntityのIDは1から始まる想定
            } while (excludedCardsThisTurn.Contains(draw)); // 除外されたカードは引かない

            var c = Instantiate(cardPrefab, PlayerHandTransform, false);

            // CardModelFactoryを使ってCardModelを生成し、カードを初期化
            CardModel cardModel = cardModelFactory.CreateFromId(draw);
            if (cardModel != null)
            {
                c.Init(cardModel);
            }
            else
            {
                Debug.LogError($"カードID {draw} の読み込みに失敗しました");
            }

            card.Add(c);
            handcard.Add(draw);
        }
    }

    /// <summary>
    /// CardNumber番目のカードを選択するメゾット
    /// </summary>
    private void CardSelect(int CardNumber)
    {
        if (CardNumber < 0 || CardNumber >= handSelected.Length || CardNumber >= card.Count)
        {
            Debug.LogError($"無効なカード番号: {CardNumber}");
            return;
        }

        // 選択がされていなかったとき
        if (!handSelected[CardNumber])
        {
            // 選択制限チェック
            if (!CanSelectCard())
            {
                Debug.Log("選択可能なカードは2枚までです。");
                return;
            }

            // 選択状態を有効化する
            handSelected[CardNumber] = true;
            ShowSelectionEffect(CardNumber);
        }
        // 選択されているとき
        else if (handSelected[CardNumber])
        {
            // 選択エフェクトを無効化する 
            HideSelectionEffect(CardNumber);
            // 選択状態を解除する
            handSelected[CardNumber] = false;
        }

        Debug.Log($"カード{CardNumber + 1}が{(handSelected[CardNumber] ? "選択" : "選択解除")}されました");
    }

    /// <summary>
    /// カードを選択できるかチェック
    /// </summary>
    private bool CanSelectCard()
    {
        int selectedCount = handSelected.Count(x => x);
        return selectedCount < 2;
    }

    /// <summary>
    /// 選択エフェクトを表示
    /// </summary>
    private void ShowSelectionEffect(int cardIndex)
    {
        if (checkEffect != null && cardIndex < card.Count)
        {
            var effect = Instantiate(checkEffect, card[cardIndex].transform.position, Quaternion.identity);
            effect.transform.SetParent(card[cardIndex].transform);
            selectionEffects[cardIndex] = effect;
        }
    }

    /// <summary>
    /// 選択エフェクトを非表示
    /// </summary>
    private void HideSelectionEffect(int cardIndex)
    {
        if (selectionEffects[cardIndex] != null)
        {
            Destroy(selectionEffects[cardIndex]);
            selectionEffects[cardIndex] = null;
        }
    }

    /// <summary>
    /// 全ての選択エフェクトをリセット
    /// </summary>
    private void ResetSelectionEffects()
    {
        for (int i = 0; i < selectionEffects.Length; i++)
        {
            if (selectionEffects[i] != null)
            {
                Destroy(selectionEffects[i]);
                selectionEffects[i] = null;
            }
        }
    }
}
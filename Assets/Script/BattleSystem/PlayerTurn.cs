// PlayerTurn.cs
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerTurn : MonoBehaviour
{
    [SerializeField] CardController cardPrefab;
    [SerializeField] Transform PlayerHandTransform;
    [SerializeField] private GameObject checkEffect;            // 選択されたカードにエフェクト付与用

    private List<CardController> card = new();                  // 初期Deck取得
    private CardModelFactory cardModelFactory;                  // カードモデル生成用

    private List<int> selectedCardsThisTurn = new List<int>();  // 選択されたカードのIDを保持
    private List<int> excludedCardsThisTurn = new List<int>();  // 破棄されたカードのIDを保持
    private List<int> handcard = new();                         // 手札のカードID
    private bool isPlayerTurn;                                  // プレイヤーのターンかどうか
    private int decksheet = 42;                                 // デッキの最大枚数
    private InputReader inputReader;                            // 入力を管理するクラス（必要に応じて実装）
    private bool inputEnabled = false;                          // 入力を受け付けるかどうかのフラ

    // カード選択状態管理
    private bool[] handSelected = new bool[3];                  // 各カード（3枚）が選択されているかどうか
    private GameObject[] selectionEffects = new GameObject[3];  // 各カードの選択エフェクト

    public event System.Action TurnFinished;

    private void Awake()
    {
        inputReader = GetComponent<InputReader>();
        inputReader.CardSelectEvent += OnCardSelect;
        inputReader.DisCardEvent += OnDisCard;

        cardModelFactory = new CardModelFactory();
    }

    private void OnDestroy()
    {
        inputReader.CardSelectEvent -= OnCardSelect;
        inputReader.DisCardEvent -= OnDisCard;
    }

    public void StartPlayerTurn()
    {
        selectedCardsThisTurn.Clear();
        excludedCardsThisTurn.Clear();
        inputEnabled = true;
        handcard.Clear();

        card.Clear();
        CreateCard();
    }

    public void FinishPlayerTurn()
    {
        inputEnabled = false;

        for (int i = 0; i < handSelected.Length; i++)
        {
            if (handSelected[i])
                selectedCardsThisTurn.Add(handcard[i]);
            else
                excludedCardsThisTurn.Add(handcard[i]);

            handSelected[i] = false;
        }

        ResetSelectionEffects();

        Debug.Log("選択カード: " + string.Join(",", selectedCardsThisTurn));
        Debug.Log("除外カード: " + string.Join(",", excludedCardsThisTurn));

        TurnFinished?.Invoke();
    }
    /// 1,2,3ボタンでカードを選択
    /// </summary>
    private void OnCardSelect(int index)
    {
        if (!inputEnabled) return;
        CardSelect(index);
    }

    private void OnDisCard()
    {
        if (!inputEnabled) return;
        DeisCard();
    }

    /// <summary>
    /// 待機しているときだけ入力受付
    /// </summary>
    /// <param name="hand"></param>
    /// <returns></returns>

    private void CreateCard()
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
    /// 
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

    /// <summary>
    /// 入力: 決定ボタンイベント
    /// </summary>
    private void DeisCard()
    {
        if (!inputEnabled) return;

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

        CreateCard();
    }
}

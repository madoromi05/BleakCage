// PlayerTurn.cs
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerTurn : MonoBehaviour
{
    [SerializeField] CardController cardPrefab;
    [SerializeField] Transform PlayerHandTransform;
    [SerializeField] private Deck deck;                         // デッキ情報を保持するクラス

    private InputReader inputReader;                            // 入力を管理するクラス
    private PlayerModel playerModel;                            // プレイヤーモデル
    private EnemyModel enemyModel;                              // 敵モデル
    private WeaponModel weaponModel;
    private CardModelFactory cardModelFactory;                  // カードモデル生成用

    private List<CardController> card = new();                  // 初期Deck取得
    private List<int> selectedCardsThisTurn = new List<int>();  // 選択されたカードのIDを保持
    private List<int> excludedCardsThisTurn = new List<int>();  // 破棄されたカードのIDを保持
    private List<int> handcard = new();                         // 手札のカードID
    private Queue<ICardCommand> commandQueue = new();           // コマンドキュー
    private bool isPlayerTurn;                                  // プレイヤーのターンかどうか
    private bool inputEnabled = false;                          // 入力を受け付けるかどうかのフラ
    private bool isProcessing = false;                          // 処理中フラグを追加
    private float lastInputTime = 0f;                           // 前回入力時刻
    private float inputCooldown = 0.1f;                         // 入力クールダウン時間（秒）


    // カード選択状態管理
    private bool[] handSelected = new bool[3];                  // 各カード（3枚）が選択されているかどうか

    public event System.Action TurnFinished;                    // ターン終了イベント

    private void Awake()
    {
        inputReader = GetComponent<InputReader>();
        inputReader.CardSelectEvent += OnCardSelect;
        inputReader.DisCardEvent += OnDisCard;

        cardModelFactory = new CardModelFactory();
    }

    public void Setup(PlayerModel playerModel, EnemyModel enemyModel, WeaponModel weaponModel, Deck deck)
    {
        this.playerModel = playerModel;
        this.enemyModel = enemyModel;
        this.weaponModel = weaponModel;
        this.deck = deck;
    }

    private void OnDestroy()
    {
        inputReader.CardSelectEvent -= OnCardSelect;
        inputReader.DisCardEvent -= OnDisCard;
    }

    public void StartPlayerTurn()
    {
        commandQueue.Clear();
        selectedCardsThisTurn.Clear();
        excludedCardsThisTurn.Clear();
        inputEnabled = true;
        handcard.Clear();

        Debug.Log("除外リストの内容（StartPlayerTurn）: " + string.Join(",", excludedCardsThisTurn));

        card.Clear();
        CreateCard();
    }

    public void FinishPlayerTurn()
    {
        // ターン終わりにCardの効果処理
        StartCoroutine(ExecuteCardCommands());
    }
    /// 1,2,3ボタンでカードを選択
    /// </summary>
    private void OnCardSelect(int index)
    {
        if (!inputEnabled || isProcessing) return;

        // クールタイム処理
        if (Time.time - lastInputTime < inputCooldown) return;
        lastInputTime = Time.time;

        isProcessing = true;  // 処理ロック

        CardSelect(index);
        isProcessing = false; // 処理ロック解除

        Debug.Log($"選択中カードID: {string.Join(",", GetCurrentlySelectedCardIds())}");
    }

    private void OnDisCard()
    {
        if (!inputEnabled || isProcessing) return;

        if (Time.time - lastInputTime < inputCooldown) return;
        lastInputTime = Time.time;

        isProcessing = true;  // 処理ロック
        DeisCard();
        isProcessing = false;
    }

    /// <summary>
    /// 待機しているときだけ入力受付
    /// </summary>
    /// <param name="hand"></param>
    /// <returns></returns>

    private void CreateCard()
    {
        // handSelectedとselectionEffectsを完全初期化
        handSelected = new bool[3];

        // 既存のカードを消す
        foreach (Transform child in PlayerHandTransform)
        {
            Destroy(child.gameObject);
        }

        card.Clear();
        handcard.Clear();

        //三枚提示
        for (int i = 0; i < 3; i++)
        {
            if (deck.TryDrawCard(excludedCardsThisTurn, out int drawId))
            {
                var c = Instantiate(cardPrefab, PlayerHandTransform, false);
                CardModel cardModel = cardModelFactory.CreateFromId(drawId);
                if (cardModel != null)
                {
                    c.Init(cardModel);
                }
                else
                {
                    Debug.LogError($"カードID {drawId} の読み込みに失敗しました");
                }

                card.Add(c);
                handcard.Add(drawId);
            }
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
        }
        // 選択されているとき
        else if (handSelected[CardNumber])
        {
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
    /// 入力: Enterボタンイベント
    /// </summary>
    private void DeisCard()
    {
        if (!inputEnabled) return;

        // 選択されているカードがない場合は何もしない
        int selectedCount = handSelected.Count(x => x);
        if (selectedCount == 0)
        {
            Debug.Log("1枚以上カードを選択してください。");
            return;
        }

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
        for (int i = 0; i < handSelected.Length; i++)
        {
            handSelected[i] = false;
        }

        CreateCard();
    }

    private IEnumerator ExecuteCardCommands()
    {
        foreach (var selectedId in selectedCardsThisTurn)
        {
            var cardModel = cardModelFactory.CreateFromId(selectedId);
            if (cardModel == null)
            {
                Debug.LogWarning($"カードID {selectedId} のカードが存在しません");
                continue;
            }

            if (cardModel.CardAttribute == AttributeType.Heal)
            {
                // 回復値は仮に0.2f割合で回復
                commandQueue.Enqueue(new HealCardCommand(playerModel, 0.2f, useRatio: true));
            }
            // Heal以外は攻撃処理
            else
            {
                commandQueue.Enqueue(new AttackCardCommand(playerModel, enemyModel, cardModel, weaponModel));
            }
        }

        // 順に実行
        while (commandQueue.Count > 0)
        {
            var command = commandQueue.Dequeue();
            command.Do();
            yield return new WaitForSeconds(0.3f); // 任意のウェイト
        }

        Debug.Log("カード効果の実行完了");

        // ターン終了イベントを発火
        TurnFinished?.Invoke();
    }

    private List<int> GetCurrentlySelectedCardIds()
    {
        List<int> selectedIds = new();
        for (int i = 0; i < handSelected.Length; i++)
        {
            if (handSelected[i] && i < handcard.Count)
            {
                selectedIds.Add(handcard[i]);
            }
        }
        return selectedIds;
    }

}

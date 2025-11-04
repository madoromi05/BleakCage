/// <summary>
/// プレイヤーのカード選択、手札管理
/// </summary>
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

public class PlayerTurn : MonoBehaviour
{
    [SerializeField] private CardController cardPrefab;
    [SerializeField] private Transform playerHandTransform;
    [SerializeField] private BattleInputReader inputReader;
    [SerializeField] private GuardGaugeSystem guardGaugeSystem;

    public event System.Action OnTurnFinished;
    public event System.Action<int> OnCardSelected;
    public bool isTurnFinished;

    // チュートリアル用のイベント
    public event System.Action<int, bool> OnCardSelectedForTutorial;  // カード選択時
    public event System.Action OnConfirmSelectionForTutorial;        // 選択確定時

    private BattleCardDeck battleDeck;
    private CardModelFactory cardModelFactory;
    private CardRuntime cardRuntime;
    private EnemyStatusUIController enemyStatusUIController;

    private List<CardController> handCardControllers = new();                       // 手札のカード表示
    private List<CardRuntime> handCards = new();                                    // 手札のカードdata
    private List<CardRuntime> selectedCardsThisTurn = new List<CardRuntime>();      // 選択されたカードのIDを保持
    private Dictionary<PlayerRuntime, List<EnemyModel>> playerTargetSelections;
    private List<System.Guid> excludedCardInstancesThisTurn = new List<System.Guid>(); // 破棄されたカードのIDを保持
    private List<EnemyStatusUIController> enemyStatusUIControllers;
    private Dictionary<EnemyModel, EnemyController> enemyControllers;

    private bool isCounterTurn = false;
    private System.Action onCounterActionFinishedCallback;
    private bool[] isCardSelected = new bool[3];      // 各カード（3枚）が選択されているかどうか
    private bool inputEnabled = false;                // ターン中全体の入力フラグ
    private bool isInputLocked = false;               // 入力受付処理中に他の入力を受け取らない
    private bool isTutorialMode = false;
    private float lastInputTime = 0f;                 // 前回入力時刻
    private float inputCooldown = 0.1f;               // 入力クールダウン時間（秒）
    private IAttackStrategy damageStrategy;
    private PlayerActionExecutor actionExecutor; // カード実行クラス

    // private AudioSource audioSource;
    // public AudioClip disposecard;
    // public AudioClip check;

    private void Awake()
    {
        inputReader.CardSelectEvent += OnCardSelect;
        inputReader.DisCardEvent += OnConfirmSelectionAndRedraw;

        damageStrategy = new AttributeWeakness();
        cardModelFactory = new CardModelFactory();

        actionExecutor = new PlayerActionExecutor(this);
        // audioSource = GetComponent<AudioSource>();
    }

    public void Setup(Dictionary<PlayerRuntime, List<EnemyModel>> playerSelections,
                BattleCardDeck battleDeck,
                List<EnemyStatusUIController> enemyUIControllers,
                Dictionary<EnemyModel, EnemyController> enemyControllers)
    {
        this.playerTargetSelections = playerSelections;
        this.battleDeck = battleDeck;
        this.enemyStatusUIControllers = enemyUIControllers;
        this.enemyControllers = enemyControllers;
    }

    public void SetTutorialMode(bool mode)
    {
        isTutorialMode = mode;
    }

    public void StartPlayerTurn()
    {
        isCounterTurn = false;
        isTurnFinished = false;
        isTurnFinished = false;
        selectedCardsThisTurn.Clear();
        excludedCardInstancesThisTurn.Clear();
        TickDownAllPlayerBuffs();
        battleDeck.ResetBattleDeck(battleDeck.battleCardDeck);
        inputEnabled = true;
        DrawHandCards();
    }

    // ターン終わりにCardの効果処理
    public void FinishPlayerTurn()
    {
        if (isTurnFinished) return;
        isTurnFinished = true;
        inputEnabled = false;

        // 手札のカード表示をdestory
        foreach (var contCard in handCardControllers)
        {
            if (contCard != null)
            {
                Destroy(contCard.gameObject);
            }
        }
        handCardControllers.Clear();

        // 実行クラスに処理を委任
        StartCoroutine(actionExecutor.ExecuteActions(
            selectedCardsThisTurn,
            playerTargetSelections,
            enemyStatusUIControllers,
            enemyControllers,
            damageStrategy,
            () => OnTurnFinished?.Invoke() // 実行完了時にイベントを発火
        ));
    }

    /// <summary>
    /// デッキから手札を3枚引き、表示する
    /// </summary>
    public void DrawHandCards()
    {
        // 既存のカードを消す
        foreach (var contCard in handCardControllers)
        {
            if (contCard != null) Destroy(contCard.gameObject);
        }

        handCardControllers.Clear();
        handCards.Clear();
        isCardSelected = new bool[3];

        //三枚提示
        for (int i = 0; i < 3; i++)
        {
            if (battleDeck.TryDrawCard(out CardRuntime drawnCard))
            {
                var cardObject = Instantiate(cardPrefab, playerHandTransform, false);
                CardModel cardModel = cardModelFactory.CreateFromID(drawnCard.ID);
                cardObject.Init(cardModel);
                handCards.Add(drawnCard);
                handCardControllers.Add(cardObject);
            }
        }
    }

    // (中略: OnCardSelect から ConfirmSelectionAndRedraw までは変更なし)

    /// <summary>
    /// 1,2,3ボタンでカードを選択
    /// </summary>
    private void OnCardSelect(int inputNumber)
    {
        if (!inputEnabled || isInputLocked) return;
        if (Time.time - lastInputTime < inputCooldown) return;

        lastInputTime = Time.time;
        isInputLocked = true;
        CardSelect(inputNumber);
        OnCardSelected?.Invoke(inputNumber);

        // audioSource.PlayOneShot(check);
        if (isTutorialMode)
        {
            OnCardSelectedForTutorial?.Invoke(inputNumber, isCardSelected[inputNumber]);
        }
        isInputLocked = false;
    }

    /// <summary>
    /// CardNumber番目のカードを選択するメゾット
    /// </summary>
    private void CardSelect(int inputNumber)
    {
        if (inputNumber < 0 || inputNumber >= isCardSelected.Length)
        {
            Debug.Log($"無効なカード番号: {inputNumber}");
            return;
        }

        if (isCardSelected[inputNumber])
        {
            isCardSelected[inputNumber] = false;
            // audioSource.PlayOneShot(disposecard);
        }
        else
        {
            // 選択制限チェック
            if (!CanSelectCard())
            {
                Debug.Log("選択可能なカードは2枚までです。");
                return;
            }

            isCardSelected[inputNumber] = true;
        }
    }

    /// <summary>
    /// カードを選択できるかチェック
    /// </summary>
    private bool CanSelectCard()
    {
        int selectedCount = isCardSelected.Count(x => x);
        return selectedCount < 2;
    }

    /// <summary>
    /// タイマーと入力管理
    /// </summary>
    private void OnConfirmSelectionAndRedraw()
    {
        if (!inputEnabled || isInputLocked) return;

        if (isTutorialMode)
        {
            OnConfirmSelectionForTutorial?.Invoke();
        }
        else
        {
            if (Time.time - lastInputTime < inputCooldown) return;
            lastInputTime = Time.time;
        }

        isInputLocked = true;
        ConfirmSelectionAndRedraw();
        isInputLocked = false;
    }

    /// <summary>
    /// カードの選択を確定し、手札を再抽選する
    /// </summary>
    private void ConfirmSelectionAndRedraw()
    {
        if (!inputEnabled) return;

        // 選択されているカードがない場合は何もしない
        int selectedCount = isCardSelected.Count(x => x);
        if (selectedCount == 0)
        {
            Debug.Log("1枚以上カードを選択してください。");
            return;
        }

        List<CardRuntime> cardsToExecute = new List<CardRuntime>();

        for (int i = 0; i < isCardSelected.Length; i++)
        {
            if (i >= handCards.Count) continue;

            CardRuntime cardInstance = handCards[i];
            if (isCardSelected[i])
            {
                if (isCounterTurn)
                {
                    cardsToExecute.Add(cardInstance);
                }
                else
                {
                    selectedCardsThisTurn.Add(cardInstance);
                }
            }
            else
            {
                Debug.Log($"破棄するカード番号: {i} (ID: {cardInstance.ID})");
                excludedCardInstancesThisTurn.Add(cardInstance.InstanceID);
            }
        }

        //選択状態をリセット
        for (int i = 0; i < isCardSelected.Length; i++)
        {
            isCardSelected[i] = false;
        }

        if (isCounterTurn)
        {
            // === カウンターアクションの場合 ===
            isCounterTurn = false; // アクション終了
            inputEnabled = false;

            // 手札のカード表示をdestory
            foreach (var contCard in handCardControllers)
            {
                if (contCard != null) Destroy(contCard.gameObject);
            }
            handCardControllers.Clear();
            handCards.Clear();

            StartCoroutine(actionExecutor.ExecuteActions(
                cardsToExecute,
                playerTargetSelections,
                enemyStatusUIControllers,
                enemyControllers,
                damageStrategy,
                () =>
                {
                    onCounterActionFinishedCallback?.Invoke();
                    onCounterActionFinishedCallback = null;
                }
            ));
        }
        else
        {
            // === 通常ターンの場合 (元のロジック) ===
            // 手札を再抽選する
            DrawHandCards();
        }
    }

    /// <summary>
    /// 全プレイヤーのバフの持続ターンを1減らし、0になったものを削除する
    /// </summary>
    private void TickDownAllPlayerBuffs()
    {
        if (playerTargetSelections == null) return;

        foreach (PlayerRuntime player in playerTargetSelections.Keys)
        {
            if (player != null)
            {
                player.BuffHandler.TickDownBuffDurations();
            }
        }
    }

    /// <summary>
    /// カウンター成功時のエクストラアクション（カード1回提示）を開始する
    /// </summary>
    /// <param name="onCounterActionFinished">このアクションが完了したときに呼ばれるコールバック</param>
    public void StartCounterAction(System.Action onCounterActionFinished)
    {
        Debug.Log("カウンターアクションを開始します");
        isCounterTurn = true;    // カウンターモードをON
        isTurnFinished = false; // ターン実行中
        inputEnabled = true;
        isInputLocked = false;

        // このコールバックを保存
        this.onCounterActionFinishedCallback = onCounterActionFinished;

        // 3枚引いて表示
        DrawHandCards();
        // ここで PlayerTurn は ConfirmSelectionAndRedraw() が呼ばれるのを待つ
    }
}
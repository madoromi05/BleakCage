/// <summary>
/// プレイヤーターンの処理
///</summary>

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class TuterealPlayerTurn : MonoBehaviour
{
    [SerializeField] private CardController cardPrefab;
    [SerializeField] private Transform playerHandTransform;
    [SerializeField] private BattleCardDeck battleDeck;
    [SerializeField] public TextMeshProUGUI tutorialWindow;     // チュートリアル説明文表示用

    public event System.Action TurnFinished;                    // ターン終了イベント

    private InputReader inputReader;
    private PlayerRuntime playerRuntime;
    private EnemyModel enemyModel;
    private WeaponRuntime weaponRuntime;
    private CardModelFactory cardModelFactory;
    private CardRuntime cardRuntime;

    private List<CardController> handCardControllers = new();   // 手札のカード表示
    private List<CardRuntime> handCards = new();                // 手札のカードdata

    private List<CardRuntime> selectedCardsThisTurn = new List<CardRuntime>();          // 選択されたカードのIDを保持
    private List<System.Guid> excludedCardInstancesThisTurn = new List<System.Guid>();  // 破棄されたカードのIDを保持
    private Queue<ICommand> commandQueue = new();               // コマンドキュー

    private bool[] isCardSelected = new bool[3];                // 各カード（3枚）が選択されているかどうか
    private bool inputEnabled = false;                          // ターン中全体の入力フラグ
    private bool isInputLocked = false;                         // 入力受付処理中に他の入力を受け取らない
    private float lastInputTime = 0f;                           // 前回入力時刻
    private float inputCooldown = 0.1f;                         // 入力クールダウン時間（秒）
    private IAttackStrategy damageStrategy;

    // === チュートリアル進行管理用のフィールド ===
    private enum TutorialStep
    {
        Start,
        ExplainSelection,      // カード選択の説明
        SelectSpecificCards,   // 特定カードの選択を待つ
        ConfirmFirstSelection, // 最初のEnterを待つ
        ExplainFreeSelection,  // 自由選択の説明
        ReadyToStartTimer,     // タイマー開始の確認を待つ
        PlayerTurnActive,      // 制限時間中のプレイヤー操作
        Finished
    }
    private TutorialStep currentStep;

    private void Awake()
    {
        inputReader = GetComponent<InputReader>();
        inputReader.CardSelectEvent += OnCardSelect;
        inputReader.DisCardEvent += OnConfirmSelectionAndRedraw;

        damageStrategy = new AttributeWeakness();
        cardModelFactory = new CardModelFactory();
    }

    private void Start()
    {
        // 最初はチュートリアルウィンドウを非表示にしておく
        if (tutorialWindow != null)
        {
            tutorialWindow.gameObject.SetActive(false);
        }
    }

    public void Setup(PlayerRuntime playerRuntime, EnemyModel enemyModel, BattleCardDeck battleDeck)
    {
        this.playerRuntime = playerRuntime;
        this.enemyModel = enemyModel;
        this.battleDeck = battleDeck;
    }
    /// <summary>
    /// チュートリアルの準備フェーズを開始する（BattleManagerから呼ばれる）
    /// </summary>
    public IEnumerator StartTutorialPhase()
    {
        // 最初の3枚を引いておく
        DrawHandCards();
        // 最初のステップへ
        currentStep = TutorialStep.Start;
        yield return StartCoroutine(UpdateTutorialStep());
    }

    /// <summary>
    /// チュートリアルのステップを進行させるコルーチン
    /// </summary>
    private IEnumerator UpdateTutorialStep()
    {
        switch (currentStep)
        {
            case TutorialStep.Start:
                tutorialWindow.gameObject.SetActive(true);
                tutorialWindow.text = "配られた3枚のスキルカードのうち、最低1枚、最大2枚を選択します。\n選択はテンキーで行い、選択が終わったらEnterキーで残ったカードを破棄します。\n選んだカードはスタックされていき、破棄されたカードは\"修復\"するまでデッキに戻りません。\n破棄が終わったら、再び3枚のスキルカードが提示されるので、制限時間が続く限りこれを繰り返します。\n\n<size=80%>（クリックで次に進む）</size>";

                // マウスクリックを待つ
                yield return new WaitUntil(() => Input.GetMouseButtonDown(0));

                currentStep = TutorialStep.ExplainSelection;
                StartCoroutine(UpdateTutorialStep()); // 次のステップへ
                break;

            case TutorialStep.ExplainSelection:
                tutorialWindow.text = "まずはこのカード2つを選択してみてください。";
                // 最初の2枚のカードを光らせる（CardControllerにHighlightメソッドが必要）
                //handCardControllers[0].Highlight(true);
                //handCardControllers[1].Highlight(true);
                currentStep = TutorialStep.SelectSpecificCards;
                break;

            case TutorialStep.SelectSpecificCards:
                // プレイヤーの入力を待つ状態。OnCardSelectで進行
                break;

            case TutorialStep.ConfirmFirstSelection:
                tutorialWindow.text = "素晴らしい！\nEnterキーで次に進みましょう";
                //handCardControllers[0].Highlight(false);
                //handCardControllers[1].Highlight(false);
                // プレイヤーの入力を待つ状態。OnConfirmSelectionAndRedrawで進行
                break;

            case TutorialStep.ExplainFreeSelection:
                tutorialWindow.text = "ここからは好きなカードを選択してください。\n選択したらEnterキーで次に進み、可能な限り多くのスキルカードを選択し、多くのダメージが与えられるように頑張りましょう！";
                yield return new WaitForSeconds(3.5f); // テキストを読む時間
                tutorialWindow.text = "次のEnterキー入力後、制限時間が開始します。";
                currentStep = TutorialStep.ReadyToStartTimer;
                break;

            case TutorialStep.ReadyToStartTimer:
                // プレイヤーの入力を待つ状態。OnConfirmSelectionAndRedrawで進行
                break;

            case TutorialStep.PlayerTurnActive:
                tutorialWindow.gameObject.SetActive(false);
                inputEnabled = true; // ここで初めて自由な入力を許可
                // このコルーチンは終了。BattleManagerのタイマーが動き出す
                break;
        }
    }

    public void StartPlayerTurn()
    {
        commandQueue.Clear();
        selectedCardsThisTurn.Clear();
        excludedCardInstancesThisTurn.Clear();

        battleDeck.ResetBattleDeck(battleDeck.battleCardDeck);　// 破棄カードをリセット
        inputEnabled = true;
        DrawHandCards();
    }

    public void FinishPlayerTurn()
    {
        // ターン終わりにCardの効果処理
        StartCoroutine(ExecuteCardCommands());
    }

    /// <summary>
    /// デッキから手札を3枚引き、表示する
    /// </summary>
    private void DrawHandCards()
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
                handCards.Add(drawnCard); // 実際のインスタンスを手札に追加
                handCardControllers.Add(cardObject);
            }
        }
    }

    /// <summary>
    /// 1,2,3ボタンでカードを選択
    /// </summary>
    private void OnCardSelect(int inputNumber)
    {
        if (!inputEnabled || isInputLocked) return;

        // クールタイム処理
        if (Time.time - lastInputTime < inputCooldown) return;
        lastInputTime = Time.time;

        isInputLocked = true;
        CardSelect(inputNumber);
        isInputLocked = false;

        Debug.Log($"選択中カードID: {string.Join(",", GetCurrentlySelectedCardIds())}");
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

        // 選択がされたとき
        if (isCardSelected[inputNumber])
        {
            isCardSelected[inputNumber] = false;
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
        Debug.Log($"カード{inputNumber + 1}が{(isCardSelected[inputNumber] ? "選択" : "選択解除")}されました");
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

        if (Time.time - lastInputTime < inputCooldown) return;
        lastInputTime = Time.time;

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

        for (int i = 0; i < isCardSelected.Length; i++)
        {
            if (i >= handCards.Count) continue;

            CardRuntime cardInstance = handCards[i];
            if (isCardSelected[i])
            {
                selectedCardsThisTurn.Add(cardInstance);
            }
            else
            {
                excludedCardInstancesThisTurn.Add(cardInstance.InstanceID);
            }
        }

        Debug.Log("選択カード: " + string.Join(",", selectedCardsThisTurn));
        Debug.Log("除外カード: " + string.Join(",", excludedCardInstancesThisTurn));

        //選択状態をリセット
        for (int i = 0; i < isCardSelected.Length; i++)
        {
            isCardSelected[i] = false;
        }

        DrawHandCards();
    }

    /// <summary>
    /// コマンドパターン呼び出し
    /// </summary>
    private IEnumerator ExecuteCardCommands()
    {
        inputEnabled = false;

        foreach (var selectedCardRuntime in selectedCardsThisTurn)
        {
            // 1. このカードがアタッチされている特定の武器を取得する
            WeaponRuntime weaponRuntime = selectedCardRuntime.weaponRuntime; // CardRuntimeが親であるWeaponRuntimeへの参照を持っている
            if (weaponRuntime == null)
            {
                Debug.LogError($"カード {selectedCardRuntime.ID} ({selectedCardRuntime.InstanceID}) はどの武器にもアタッチされていません！");
                continue;
            }

            // 2. その武器を所持しているプレイヤーを取得する
            PlayerRuntime playerRuntime = weaponRuntime.ParentPlayer; // WeaponRuntimeが親であるPlayerRuntimeへの参照を持っている
            if (playerRuntime == null)
            {
                Debug.LogError($"武器 {weaponRuntime.ID} ({weaponRuntime.InstanceID}) はどのプレイヤーにも所持されていません！");
                continue;
            }

            if (selectedCardRuntime.attribute == AttributeType.Heal)
            {
                // 回復値は仮に0.2f割合で回復
                // commandQueue.Enqueue(new HealCardCommand(playerModel, 0.2f, useRatio: true));
            }
            // Heal以外は攻撃処理
            else
            {
                commandQueue.Enqueue(new AttackCardCommand(playerRuntime, weaponRuntime, selectedCardRuntime, enemyModel, damageStrategy));
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
        List<int> selectedIDs = new();
        for (int i = 0; i < isCardSelected.Length; i++)
        {
            if (isCardSelected[i] && i < handCards.Count)
            {
                selectedIDs.Add(handCards[i].ID);
            }
        }
        return selectedIDs;
    }
}

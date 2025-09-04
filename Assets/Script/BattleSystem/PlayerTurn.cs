/// <summary>
/// プレイヤーターンの処理
///</summary>

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class PlayerTurn : MonoBehaviour
{
    [SerializeField] private CardController cardPrefab;
    [SerializeField] private Transform playerHandTransform;
    [SerializeField] private BattleCardDeck battleDeck;

    public event System.Action OnTurnFinished;
    public event System.Action<int> OnCardSelected;
    public event System.Action<int, bool> OnCardSelectedForTutorial;

    private BattleInputReader inputReader;
    private PlayerRuntime playerRuntime;
    private EnemyModel enemyModel;
    private WeaponRuntime weaponRuntime;
    private CardModelFactory cardModelFactory;
    private CardRuntime cardRuntime;

    private List<CardController> handCardControllers = new();   // 手札のカード表示
    private List<CardRuntime> handCards = new();                // 手札のカードdata

    private List<CardRuntime> selectedCardsThisTurn = new List<CardRuntime>();          // 選択されたカードのIDを保持
    private List<System.Guid> excludedCardInstancesThisTurn = new List<System.Guid>();  // 破棄されたカードのIDを保持
    private Queue<ICommand> commandQueue = new();

    private bool[] isCardSelected = new bool[3];                // 各カード（3枚）が選択されているかどうか
    private bool inputEnabled = false;                          // ターン中全体の入力フラグ
    private bool isInputLocked = false;                         // 入力受付処理中に他の入力を受け取らない
    private float lastInputTime = 0f;                           // 前回入力時刻
    private float inputCooldown = 0.1f;                         // 入力クールダウン時間（秒）
    private bool isTutorialMode = false;
    private IAttackStrategy damageStrategy;

    private void Awake()
    {
        inputReader = GetComponent<BattleInputReader>();
        inputReader.CardSelectEvent += OnCardSelect;
        inputReader.DisCardEvent += OnConfirmSelectionAndRedraw;

        damageStrategy = new AttributeWeakness();
        cardModelFactory = new CardModelFactory();
    }

    public void Setup(PlayerRuntime playerRuntime, EnemyModel enemyModel, BattleCardDeck battleDeck)
    {
        this.playerRuntime = playerRuntime;
        this.enemyModel = enemyModel;
        this.battleDeck = battleDeck;
    }

    public void SetTutorialMode(bool mode)
    {
        isTutorialMode = mode;
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
        OnCardSelected?.Invoke(inputNumber);

        if (isTutorialMode)
        {
            OnCardSelectedForTutorial?.Invoke(inputNumber, isCardSelected[inputNumber]);
        }
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

        if (isTutorialMode)
        {
            // チュートリアル中は、このメソッドで再抽選を行わない
            return;
        }
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
        //Debug.Log("除外カード: " + string.Join(",", excludedCardInstancesThisTurn));

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
                commandQueue.Enqueue(new AttackCommand(playerRuntime, weaponRuntime, selectedCardRuntime, enemyModel, damageStrategy));
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
        OnTurnFinished?.Invoke();
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

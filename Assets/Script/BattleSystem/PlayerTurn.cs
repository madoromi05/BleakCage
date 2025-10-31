/// <summary>
/// プレイヤーのカード、攻撃処理
///</summary>

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

    public event System.Action OnTurnFinished;
    public event System.Action<int> OnCardSelected;
    public bool isTurnFinished;

    private BattleCardDeck battleDeck;
    private CardModelFactory cardModelFactory;
    private CardRuntime cardRuntime;
    private EnemyStatusUIController enemyStatusUIController;

    private List<CardController> handCardControllers = new();                           // 手札のカード表示
    private List<CardRuntime> handCards = new();                                        // 手札のカードdata
    private List<CardRuntime> selectedCardsThisTurn = new List<CardRuntime>();          // 選択されたカードのIDを保持
    private Dictionary<PlayerRuntime, List<EnemyModel>> playerTargetSelections;
    private List<System.Guid> excludedCardInstancesThisTurn = new List<System.Guid>();  // 破棄されたカードのIDを保持
    private List<EnemyStatusUIController> enemyStatusUIControllers;
    private Queue<ICommand> commandQueue = new();

    private bool[] isCardSelected = new bool[3];                // 各カード（3枚）が選択されているかどうか
    private bool inputEnabled = false;                          // ターン中全体の入力フラグ
    private bool isInputLocked = false;                         // 入力受付処理中に他の入力を受け取らない
    private bool isTutorialMode = false;
    private float lastInputTime = 0f;                           // 前回入力時刻
    private float inputCooldown = 0.1f;                         // 入力クールダウン時間（秒）
    private IAttackStrategy damageStrategy;

    // チュートリアル用のイベント
    public event System.Action<int, bool> OnCardSelectedForTutorial;    // カード選択時
    public event System.Action OnConfirmSelectionForTutorial;           // 選択確定時

    //private AudioSource audioSource;
    //public AudioClip disposecard;
    //public AudioClip check;

    private void Awake()
    {
        inputReader.CardSelectEvent += OnCardSelect;
        inputReader.DisCardEvent += OnConfirmSelectionAndRedraw;

        damageStrategy = new AttributeWeakness();
        cardModelFactory = new CardModelFactory();
        // audioSource = GetComponent<AudioSource>();
    }

    public void Setup(Dictionary<PlayerRuntime, List<EnemyModel>> playerSelections,
                  BattleCardDeck battleDeck, List<EnemyStatusUIController> enemyUIControllers)
    {
        this.playerTargetSelections = playerSelections;
        this.battleDeck = battleDeck;
        this.enemyStatusUIControllers = enemyUIControllers;
    }

    public void SetTutorialMode(bool mode)
    {
        isTutorialMode = mode;
    }

    public void StartPlayerTurn()
    {
        isTurnFinished = false;
        commandQueue.Clear();
        selectedCardsThisTurn.Clear();
        excludedCardInstancesThisTurn.Clear();

        battleDeck.ResetBattleDeck(battleDeck.battleCardDeck);　// 破棄カードをリセット
        inputEnabled = true;
        DrawHandCards();
    }

    // ターン終わりにCardの効果処理
    public void FinishPlayerTurn()
    {
        if (isTurnFinished) return;
        isTurnFinished = true;
        inputEnabled = false;
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
                Debug.Log($"破棄するカード番号: {i} (ID: {cardInstance.ID})");
                excludedCardInstancesThisTurn.Add(cardInstance.InstanceID);
            }
        }

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
        // 手札のカード表示をdestory
        foreach (var contCard in handCardControllers)
        {
            if (contCard != null)
            {
                Destroy(contCard.gameObject);
            }
        }

        Debug.Log("実行するカード: " + string.Join(",", selectedCardsThisTurn.Select(c => c.ID)));

        inputEnabled = false;

        foreach (var selectedCardRuntime in selectedCardsThisTurn)
        {
            // 1. このカードがアタッチされている特定の武器を取得する
            WeaponRuntime weaponRuntime = selectedCardRuntime.weaponRuntime;

            // 2. その武器を所持しているプレイヤーを取得する
            PlayerRuntime attackPlayer = weaponRuntime.ParentPlayer;
            if (attackPlayer == null || weaponRuntime == null)
            {
                Debug.LogError($"カード {selectedCardRuntime.ID} ({selectedCardRuntime.InstanceID}) はどの武器にもアタッチされていません！");
                Debug.LogError($"武器 {weaponRuntime.ID} ({weaponRuntime.InstanceID}) はどのプレイヤーにも所持されていません！");
                continue;
            }

            // 2. そのプレイヤーが選択した攻撃対象リストを取得
            if (playerTargetSelections.TryGetValue(attackPlayer, out List<EnemyModel> targets))
            {
                // 3. 将来的な実装を考慮し、カードの属性をチェック
                if (selectedCardRuntime.attribute == AttributeType.Heal)
                {
                    // --- 回復コマンドの処理 ---
                    // 現状では自分自身を回復する想定
                    Debug.Log($"{attackPlayer.PlayerModel.PlayerName} が回復カードを使用。");
                    // commandQueue.Enqueue(new HealCommand(attackingPlayer, 0.2f)); // 例：最大HPの20%回復
                }
                else
                {
                    // --- 攻撃コマンドの処理 ---
                    EnemyModel finalTarget = null;
                    foreach (var potentialTarget in targets)
                    {
                        if (potentialTarget.EnemyHP > 0)
                        {
                            finalTarget = potentialTarget;
                            break;
                        }
                    }
                    if (finalTarget != null)
                    {
                        EnemyStatusUIController targetEnemyUI = enemyStatusUIControllers.FirstOrDefault(ui => ui.GetEnemyModel() == finalTarget);
                        if (targetEnemyUI != null)
                        {
                            commandQueue.Enqueue(new AttackCommand(attackPlayer, weaponRuntime, selectedCardRuntime,
                                                targetEnemyUI, finalTarget, damageStrategy));
                        }
                        else
                        {
                            Debug.LogWarning($"攻撃対象 {finalTarget.EnemyName} (ID: {finalTarget.EnemyID}) のUIが見つかりません。");
                        }
                    }
                    else
                    {
                        // 優先順位リストにいた敵が全員、このカードの処理時点までに倒された場合
                        Debug.LogWarning($"プレイヤー {attackPlayer.PlayerModel.PlayerName} の攻撃対象 (優先順位リスト) は全員倒されています。攻撃をスキップします。");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"プレイヤー {attackPlayer.PlayerModel.PlayerName} の攻撃対象が設定されていません。");
            }
        }

        // 順に実行
        while (commandQueue.Count > 0)
        {
            var command = commandQueue.Dequeue();
            command.Do();
            yield return new WaitForSeconds(0.3f);
        }

        Debug.Log("カード効果の実行完了");

        OnTurnFinished?.Invoke();
    }
}

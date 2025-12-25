/// <summary>
/// プレイヤーのカード選択、手札管理
/// </summary>
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI; // UI操作のために追加

public class PlayerTurn : MonoBehaviour
{
    [SerializeField] private CardController cardPrefab;
    [SerializeField] private Transform playerHandTransform;
    [SerializeField] private BattleInputReader inputReader;
    [SerializeField] private GuardGaugeSystem guardGaugeSystem;
    [SerializeField] private DamageCalculator damageCalculator;
    [SerializeField] private BattleCardPresenter battleCardPresenter;
    [SerializeField] private float cardSelectionYOffset = 30.0f;

    public event System.Action OnTurnFinished;
    public event System.Action<int> OnCardSelected;
    public bool isTurnFinished;

    // チュートリアル用のイベント
    public event System.Action<int, bool> OnCardSelectedForTutorial;  // カード選択時
    public event System.Action OnConfirmSelectionForTutorial;         // 選択確定時

    private BattleCardDeck battleDeck;
    private CardModelFactory cardModelFactory;
    private CardRuntime cardRuntime;
    private EnemyStatusUIController enemyStatusUIController;
    private List<PlayerStatusUIController> playerStatusUIControllers;

    private List<CardController> handCardControllers = new();                       // 手札のカード表示
    private List<CardRuntime> handCards = new();                                    // 手札のカードdata
    private List<CardRuntime> selectedCardsThisTurn = new List<CardRuntime>();      // 選択されたカードのIDを保持
    private Dictionary<int, List<EnemyModel>> playerTargetSelections;
    private List<System.Guid> excludedCardInstancesThisTurn = new List<System.Guid>(); // 破棄されたカードのIDを保持
    private List<EnemyStatusUIController> enemyStatusUIControllers;
    private Dictionary<EnemyModel, EnemyController> enemyControllers;
    private List<EnemyRuntime> allEnemyRuntimes;
    private List<Vector3> cardInitialPositions = new List<Vector3>();

    private bool isCounterTurn = false;
    private System.Action onCounterActionFinishedCallback;
    private bool[] isCardSelected = new bool[3];      // 各カード（3枚）が選択されているかどうか
    private bool inputEnabled = false;                // ターン中全体の入力フラグ
    private bool isInputLocked = false;               // 入力受付処理中に他の入力を受け取らない
    private bool isTutorialMode = false;
    private float lastInputTime = 0f;
    private float inputCooldown = 0.1f;
    private PlayerActionExecutor actionExecutor;
    private List<PlayerRuntime> allPlayers;

    private AudioSource audioSource;
    public AudioClip disposecard;
    public AudioClip check;

    // --- UI追加 ---
    [SerializeField] private GameObject EnterUI;
    [SerializeField] private GameObject key1UI;
    [SerializeField] private GameObject key2UI;
    [SerializeField] private GameObject key3UI;
    private Dictionary<int, GameObject> keyUI;
    private List<GameObject> activeKeyUIObjects = new List<GameObject>();
    // --------------

    private void Awake()
    {
        inputReader.CardSelectEvent += OnCardSelect;
        inputReader.DisCardEvent += OnConfirmSelectionAndRedraw;

        cardModelFactory = new CardModelFactory();
        actionExecutor = new PlayerActionExecutor(this);
        audioSource = GetComponent<AudioSource>();
        if (battleCardPresenter != null)
        {
            battleCardPresenter.Setup(cardModelFactory, playerHandTransform, cardPrefab);
        }
        else
        {
            Debug.LogError("BattleCardPresenter がインスペクターで設定されていません！");
        }

        // --- UI初期化 ---
        keyUI = new Dictionary<int, GameObject>();
        keyUI[0] = key1UI;
        keyUI[1] = key2UI;
        keyUI[2] = key3UI;
        if (EnterUI != null) EnterUI.SetActive(false);
        // ----------------
    }

    public void Setup(Dictionary<int, List<EnemyModel>> playerSelections,
                      List<PlayerRuntime> allPlayers,
                      BattleCardDeck battleDeck,
                      List<EnemyStatusUIController> enemyUIControllers,
                      List<PlayerStatusUIController> playerUIControllers,
                      Dictionary<EnemyModel, EnemyController> enemyControllers,
                      List<EnemyRuntime> enemyRuntimes)
    {
        this.playerTargetSelections = playerSelections;
        this.allPlayers = allPlayers;
        this.battleDeck = battleDeck;
        this.enemyStatusUIControllers = enemyUIControllers;
        this.playerStatusUIControllers = playerUIControllers;
        this.enemyControllers = enemyControllers;
        this.allEnemyRuntimes = enemyRuntimes;
    }

    public void SetTutorialMode(bool mode)
    {
        isTutorialMode = mode;
    }

    public void StartPlayerTurn()
    {
        isCounterTurn = false;
        isTurnFinished = false;
        selectedCardsThisTurn.Clear();
        excludedCardInstancesThisTurn.Clear();
        cardInitialPositions.Clear();
        TickDownAllPlayerBuffs();
        battleDeck.ResetBattleDeck(battleDeck.battleCardDeck);
        inputEnabled = true;

        if (EnterUI != null) EnterUI.SetActive(false); // UI非表示

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

        // --- キーガイドUIの削除 ---
        foreach (var keyObj in activeKeyUIObjects)
        {
            if (keyObj != null) Destroy(keyObj);
        }
        activeKeyUIObjects.Clear();
        // ------------------------

        StartCoroutine(actionExecutor.ExecuteActions(
           selectedCardsThisTurn,
           playerTargetSelections,
           enemyStatusUIControllers,
           playerStatusUIControllers,
           enemyControllers,
           allEnemyRuntimes,
           damageCalculator,
           battleCardPresenter.ShowCard,
           battleCardPresenter.HideCard,
           () => OnTurnFinished?.Invoke()
       ));
    }

    /// <summary>
    /// デッキから手札を3枚引き、表示する
    /// </summary>
    public void DrawHandCards()
    {
        foreach (var contCard in handCardControllers)
        {
            if (contCard != null) Destroy(contCard.gameObject);
        }

        handCardControllers.Clear();

        // --- キーガイドUIのリセット ---
        foreach (var keyObj in activeKeyUIObjects)
        {
            if (keyObj != null) Destroy(keyObj);
        }
        activeKeyUIObjects.Clear();
        // ----------------------------

        handCards.Clear();
        isCardSelected = new bool[3];
        int drawnCardCount = 0;

        //三枚提示
        for (int i = 0; i < 3; i++)
        {
            if (battleDeck.TryDrawCard(out CardRuntime drawnCard))
            {
                var cardObject = Instantiate(cardPrefab, playerHandTransform, false);
                CardModel cardModel = cardModelFactory.CreateFromID(drawnCard.ID);
                float basePower = drawnCard.weaponRuntime.ParentPlayer.Level + drawnCard.weaponRuntime.attackPower;
                cardObject.Init(cardModel, basePower);
                handCards.Add(drawnCard);
                handCardControllers.Add(cardObject);
                drawnCardCount++;

                // --- キーガイドUIの生成と配置 ---
                if (keyUI.ContainsKey(i))
                {
                    GameObject keyObj = Instantiate(keyUI[i], cardObject.transform, false);
                    keyObj.SetActive(true);

                    RectTransform rt = keyObj.GetComponent<RectTransform>();
                    // 位置調整 (カードの上部に表示するよう設定)
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0, 190); // 必要に応じてY座標を調整

                    activeKeyUIObjects.Add(keyObj);
                }
                // --------------------------------
            }
        }

        if (drawnCardCount == 0)
        {
            if (!isCounterTurn)
            {
                FinishPlayerTurn();
            }
            return;
        }

        Canvas canvas = playerHandTransform.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Canvas.ForceUpdateCanvases();
        }
        else
        {
            var layoutGroup = playerHandTransform.GetComponent<UnityEngine.UI.LayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.CalculateLayoutInputHorizontal();
                layoutGroup.CalculateLayoutInputVertical();
                layoutGroup.SetLayoutHorizontal();
                layoutGroup.SetLayoutVertical();
            }
        }
        cardInitialPositions.Clear();
        for (int i = 0; i < handCardControllers.Count; i++)
        {
            cardInitialPositions.Add(Vector3.zero);
        }
        UpdateAllCardVisuals();
    }

    /// <summary>
    /// 1,2,3ボタンでカードを選択
    /// </summary>
    private void OnCardSelect(int inputNumber)
    {
        if (!inputEnabled)
        {
            Debug.LogWarning($"入力が無効です (inputEnabled=false)");
            return;
        }
        if (isInputLocked)
        {
            Debug.LogWarning($"入力がロックされています (isInputLocked=true)");
            return;
        }
        if (Time.time - lastInputTime < inputCooldown) return;

        lastInputTime = Time.time;
        isInputLocked = true;
        CardSelect(inputNumber);
        OnCardSelected?.Invoke(inputNumber);

        // --- 選択したキーのUIを非表示にする (トグルで戻す処理はCardSelect/Visualsで行う) ---
        // if (inputNumber < activeKeyUIObjects.Count && activeKeyUIObjects[inputNumber] != null)
        // {
        //     activeKeyUIObjects[inputNumber].SetActive(!isCardSelected[inputNumber]);
        // }
        // -----------------------------------------------------------------------------

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
        if (inputNumber < 0 || inputNumber >= isCardSelected.Length ||
            inputNumber >= handCardControllers.Count || inputNumber >= cardInitialPositions.Count)
        {
            Debug.Log($"無効なカード番号: {inputNumber}");
            return;
        }

        if (isCardSelected[inputNumber])
        {
            isCardSelected[inputNumber] = false;
            if (audioSource != null && disposecard != null) audioSource.PlayOneShot(disposecard);
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

        UpdateAllCardVisuals();
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
                // Debug.Log($"破棄するカード番号: {i} (ID: {cardInstance.ID})");
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
            // カウンターアクションの場合
            isCounterTurn = false; // アクション終了
            inputEnabled = false;

            // 手札のカード表示をdestory
            foreach (var contCard in handCardControllers)
            {
                if (contCard != null) Destroy(contCard.gameObject);
            }
            handCardControllers.Clear();

            // UI破棄
            foreach (var keyObj in activeKeyUIObjects)
            {
                if (keyObj != null) Destroy(keyObj);
            }
            activeKeyUIObjects.Clear();

            handCards.Clear();

            StartCoroutine(actionExecutor.ExecuteActions(
                cardsToExecute,
                playerTargetSelections,
                enemyStatusUIControllers,
                playerStatusUIControllers,
                enemyControllers,
                allEnemyRuntimes,
                damageCalculator,
                battleCardPresenter.ShowCard,
                battleCardPresenter.HideCard,
                () =>
                {
                    onCounterActionFinishedCallback?.Invoke();
                    onCounterActionFinishedCallback = null;
                    OnTurnFinished?.Invoke(); // カウンター終了時もターン終了扱いにするなら必要
                }
            ));
        }
        else
        {
            if (!isTutorialMode)
            {
                DrawHandCards();
            }
            else
            {
                UpdateAllCardVisuals();
            }
        }
    }

    /// <summary>
    /// 全プレイヤーのバフの持続ターンを1減らし、0になったものを削除する
    /// </summary>
    private void TickDownAllPlayerBuffs()
    {
        if (allPlayers == null) return;

        foreach (PlayerRuntime player in allPlayers)
        {
            if (player != null)
            {
                //player.BuffHandler.TickDownBuffDurations();
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
        isTurnFinished = false;  // ターン実行中
        inputEnabled = true;
        isInputLocked = false;

        // このコールバックを保存
        this.onCounterActionFinishedCallback = onCounterActionFinished;

        // 3枚引いて表示
        DrawHandCards();
    }

    /// <summary>
    /// 手札のすべてのカードのビジュアル（選択時のYオフセット、選択可否の見た目）を更新する
    /// </summary>
    private void UpdateAllCardVisuals()
    {
        int selectedCount = isCardSelected.Count(x => x);

        // 2枚（最大数）選択されているか
        bool maxCardsSelected = (selectedCount >= 2);

        // --- Enter UIの表示制御 ---
        if (EnterUI != null)
        {
            EnterUI.SetActive(selectedCount > 0);
        }

        for (int i = 0; i < handCardControllers.Count; i++)
        {
            if (handCardControllers[i] == null) continue;

            CardController cardObject = handCardControllers[i];

            // --- キーガイドの表示制御 ---
            if (i < activeKeyUIObjects.Count && activeKeyUIObjects[i] != null)
            {
                activeKeyUIObjects[i].SetActive(!isCardSelected[i]);
            }

            bool shouldBeInteractable = true;
            if (!isCardSelected[i] && maxCardsSelected)
            {
                shouldBeInteractable = false;
            }

            cardObject.SetInteractable(shouldBeInteractable);


            Transform visualRoot = cardObject.transform.Find("VisualRoot");
            if (visualRoot == null)
            {
                Debug.LogError("CardController プレハブに 'VisualRoot' という名前の子オブジェクトが見つかりません！");
                continue;
            }

            if (isCardSelected[i])
            {
                visualRoot.localPosition = new Vector3(0, cardSelectionYOffset, 0);
            }
            else
            {
                visualRoot.localPosition = Vector3.zero;
            }
        }
    }
}
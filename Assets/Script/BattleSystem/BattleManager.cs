using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Battleのターン、順番とデータ管理
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("コンポーネント参照")]
    [SerializeField] public BattleCardDeck battleCardDeck;
    [SerializeField] private PlayerTurn playerTurn;
    [SerializeField] private EnemyTurn enemyTurn;
    [SerializeField] private SelectTurn selectTurn;

    [Header("プレファブ と キャラ出現地点")]
    [SerializeField] private GameObject enemyBasePrefab;
    [SerializeField] private GameObject playerBasePrefab;
    [SerializeField] private List<Transform> playerPositions;
    [SerializeField] private List<Transform> enemyPositions;
    [SerializeField] private GameObject playerStatusUIPrefab;
    [SerializeField] private GameObject enemyStatusUIPrefab;

    [Header("UI関連")]
    [SerializeField] private Transform playerStatusBarTransform;
    [SerializeField] private Transform enemyStatusBarTransform;
    [SerializeField] private Text timeText;
    [SerializeField] private GameObject selectionChoicePanel;
    [SerializeField] private Button keepSelectionsButton;
    [SerializeField] private Button changeSelectionsButton;
    [SerializeField] private float playerTurnDuration = 10f;
    [SerializeField] private Slider guardGaugeSlider;
    [SerializeField] private PhaseAnnouncementUIController phaseUI;
    [SerializeField] private GameObject targetMarkerPrefab;
    [SerializeField] private Text defenseFeedbackText;
    [SerializeField] private float feedbackDisplayDuration = 1.5f;

    [Header("ゲーム内データ")]
    [SerializeField] private List<StageEnemyData> allStageEnemyData;

#if TUTORIAL_ENABLED
    [Header("チュートリアル用コンポーネント")]
    [SerializeField] private GameObject tutorialObjectsParent; // チュートリアル関連オブジェクトの親
    [SerializeField] private GameObject tutorialUIPanel;
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private SelectTurnTutorialManager selectTurnTutorialManager;
    [SerializeField] private EnemyTurnTutorialManager enemyTurnTutorialManager;
    [SerializeField] private TutorialInputReader tortrialInputReader;
#endif

    public float GetCurrentGuardGauge() { return currentGuardGauge; }
    public const float GUARD_COST = 25f;                   //消費コスト
    //=================================================================================
    // Private Variables
    //=================================================================================
    private List<PlayerRuntime> players;
    private List<EnemyModel> enemies = new List<EnemyModel>();
    private List<PlayerStatusUIController> playerStatusUIs = new List<PlayerStatusUIController>();
    private List<EnemyStatusUIController> enemyStatusUIs = new List<EnemyStatusUIController>();
    private Dictionary<PlayerModel, PlayerController> playerControllers = new Dictionary<PlayerModel, PlayerController>();
    private Dictionary<EnemyModel, EnemyController> enemyControllers = new Dictionary<EnemyModel, EnemyController>();
    private EnemyModel enemyModel;
    private StageEnemyData currentStage;
    private EnemyStatusUIController enemyUIController;
    private float turnTime = 10f;
    private bool isTutorialMode = false;
    private IPhase currentPhase;
    private bool isFirstSelectionPhase = true; // 最初の選択フェーズかどうかを判定するフラグ
    private Coroutine selectionChoiceCoroutine; // 選択待機コルーチンを保持する変数
    private GameObject markerInstance;

    private float currentGuardGauge;
    private int counterCount = 0;
    private bool isExtraTurnSegmentFinished = false; // エクストラターン用フラグ
    private const float MAX_GUARD_GAUGE = 100f;
    private int currentTurn = 1;
    private Coroutine feedbackCoroutine;
    //=================================================================================
    // ライフサイクル (Startを分割)
    //=================================================================================

    void Start()
    {
        if (targetMarkerPrefab != null)
        {
            // BattleManagerの子として生成
            markerInstance = Instantiate(targetMarkerPrefab, Vector3.zero, Quaternion.identity, this.transform);
            markerInstance.SetActive(false);
        }
        if (defenseFeedbackText != null)
        {
            defenseFeedbackText.gameObject.SetActive(false);
        }
        currentGuardGauge = MAX_GUARD_GAUGE;
        UpdateGuardGaugeUI();
        LoadGameData();
        SetupStageAndCharacters();
        InitializeBattlePhases();
        StartSelectionPhase();
    }

    /// <summary>
    /// プレイヤーデータとカードデッキをロード・初期化
    /// </summary>
    private void LoadGameData()
    {
        var dataLoader = new PlayerDataLoader();
        DeckSetupRepository setupData = dataLoader.LoadPlayerPartyAndCards();
        players = setupData.Party;
        List<CardRuntime> allCardsForDeck = setupData.AllCards;

        if (allCardsForDeck == null || allCardsForDeck.Count == 0)
        {
            Debug.LogError("デッキにセットするカードが1枚もありません！ PlayerDataLoader の処理を確認してください。");
            return;
        }
        battleCardDeck.InitFromCardList(allCardsForDeck);
    }

    /// <summary>
    /// ステージデータをロードし、プレイヤーと敵を生成・配置
    /// </summary>
    private void SetupStageAndCharacters()
    {
        stageSet();
        enemyView();
        isTutorialMode = enemies.Count > 0 && enemies[0].EnemyID == 0;
        playerView();
    }

    /// <summary>
    /// 通常モードとチュートリアルモードの判定と、各ターンのセットアップ
    /// </summary>
    private void InitializeBattlePhases()
    {
        if (isTutorialMode)
        {
#if TUTORIAL_ENABLED
            if (tutorialObjectsParent != null)
            {
                tutorialObjectsParent.SetActive(true);
            }
            if (enemyTurnTutorialManager != null)
            {
                enemyTurnTutorialManager.Initialize();
            }
            selectTurnTutorialManager.Initialize(tortrialInputReader, players, enemies, playerStatusUIs, enemyStatusUIs);
            currentPhase = selectTurnTutorialManager;
            currentPhase.OnPhaseFinished += OnSelectionPhaseFinished;
#else
            Debug.LogWarning("チュートリアルモード（EnemyID 0）で戦闘が開始されましたが、TUTORIAL_ENABLED シンボルが定義されていません。");
            isTutorialMode = false; // 通常モードとして続行
            InitializeNonTutorialPhases(); //通常モードの初期化を呼ぶ
#endif
        }
        // チュートリアル用オブジェクトを非表示にする
        else
        {
            if (tutorialObjectsParent != null)
            {
                tutorialObjectsParent.SetActive(false);
            }
            InitializeNonTutorialPhases(); // 通常モードの初期化を呼ぶ
        }

        if (selectionChoicePanel != null)
        {
            selectionChoicePanel.SetActive(false);
        }

        // 敵ターンのセットアップ (チュートリアル/通常モード共通)
        List<PlayerModel> playerModels = players.Select(p => p.PlayerModel).ToList();
        enemyTurn.EnemySetup(playerModels, enemies, enemyControllers, playerControllers, playerStatusUIs);
        enemyTurn.TurnFinished += OnEnemyTurnFinished;
    }

    /// <summary>
    /// 通常モード（非チュートリアル）のフェーズ初期化
    /// </summary>
    private void InitializeNonTutorialPhases()
    {
        selectTurn.Initialize(players, enemies, playerStatusUIs, enemyStatusUIs);
        currentPhase = selectTurn;
        currentPhase.OnPhaseFinished += OnSelectionPhaseFinished;
    }

    //=================================================================================
    // キャラクターとステージのセットアップ
    //=================================================================================
    private void playerView()
    {
        // --- 共通：プレイヤーを1体だけ生成する ---
        Debug.Log("プレイヤー生成処理：最初の1体のみ生成します。");

        // チュートリアル・通常モード共通で PlayerID=1 を優先的に探す
        PlayerRuntime targetPlayer = players.FirstOrDefault(p => p.PlayerModel.PlayerID == 1);

        // 見つからなければ先頭のプレイヤーを代用
        if (targetPlayer == null && players.Count > 0)
        {
            targetPlayer = players[0];
            Debug.LogWarning("PlayerID 1 が見つからなかったため、パーティ先頭のプレイヤーを使用します。");
        }

        if (targetPlayer == null)
        {
            Debug.LogError("プレイヤーデータが存在しません！ PlayerDataLoader の処理を確認してください。");
            return;
        }

        // 出現位置を確認
        if (playerPositions.Count == 0 || playerPositions[0] == null)
        {
            Debug.LogError("プレイヤー出現位置が設定されていません！（playerPositions[0] が null）");
            return;
        }

        // 実際に生成
        Transform spawnPoint = playerPositions[0];
        SpawnPlayerCharacter(targetPlayer, spawnPoint);

        Debug.Log($"プレイヤー '{targetPlayer.PlayerModel.PlayerName}' を {spawnPoint.name} に生成しました。");
    }


    private void stageSet()
    {
        // 挑戦する敵のデータをステージIDから取得
        int currentStageID = StageManager.SelectedStageID;

        if (currentStageID == -1)
        {
            Debug.LogError("ステージIDが設定されていません！StageManager.SelectedStageIDを確認してください。");
#if UNITY_EDITOR
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName.Contains("Tutorial"))
            {
                currentStageID = 0; // チュートリアル
                Debug.LogWarning($"シーン名'{sceneName}'のため、エディタ専用フォールバックとしてID '0' (チュートリアル) を使用します。");
            }
            else
            {
                currentStageID = 1; // 通常ステージ
                Debug.LogWarning($"シーン名'{sceneName}'のため、エディタ専用フォールバックとしてID '1' (通常) を使用します。");
            }
#endif
        }

        currentStage = allStageEnemyData.FirstOrDefault(stage => stage.stageEnemyID == currentStageID);

        if (currentStage == null)
        {
            Debug.LogError($"ID {currentStageID} に一致する StageEnemyData が 'allStageEnemyData' に見つかりません！ BattleManager の Inspector を確認してください。", this);
            this.enabled = false;
            return;
        }
    }

    private void enemyView()
    {
        // 敵の生成
        var enemyFactory = new EnemyModelFactory();
        for (int i = 0; i < currentStage.enemyIDs.Count; i++)
        {
            // 定義されたポジション数を超えないようにチェック
            if (i >= enemyPositions.Count)
            {
                Debug.LogError($"敵 {i} のためのポジションが定義されていません。");
                break;
            }

            int enemyId = currentStage.enemyIDs[i];
            EnemyModel enemy = enemyFactory.CreateFromId(enemyId);
            enemies.Add(enemy);

            if (enemyBasePrefab == null)
            {
                Debug.LogError("BattleManagerに 'Enemy Base Prefab' が設定されていません！", this);
                break;
            }

            // 指定された3D座標に敵を生成
            Transform spawnPoint = enemyPositions[i];
            var enemyObject = Instantiate(enemyBasePrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
            EnemyController enemyController = enemyObject.GetComponent<EnemyController>();
            if (enemyController == null)
            {
                Debug.LogError($"プレハブ '{enemyBasePrefab.name}' のルートに EnemyController スクリプトがアタッチされていません！", enemyObject);
                continue;
            }
            if (playerPositions.Count > 0 && playerPositions[0] != null)
            {
                enemyController.Init(enemy, playerPositions[0]);
            }
            else
            {
                Debug.LogError("playerPositions[0] が設定されていません！ EnemyController の Init に null を渡します。");
                enemyController.Init(enemy, null);
            }
            enemyControllers.Add(enemy, enemyController);
            // 敵のStatusUI生成
            var statusUIObject = Instantiate(enemyStatusUIPrefab, enemyStatusBarTransform, false);
            EnemyStatusUIController uiController = statusUIObject.GetComponent<EnemyStatusUIController>();
            uiController.SetEnemyStatus(enemy);
            enemyController.SetStatusUI(uiController);
            enemyStatusUIs.Add(uiController);
        }
    }

    //=================================================================================
    // ターン進行とフェーズ管理
    //=================================================================================
    /// <summary>
    /// ターン数とフェーズ名をUIに表示する (前回の提案)
    /// </summary>
    private IEnumerator ShowPhaseUI(string phaseName)
    {
        if (phaseUI != null)
        {
            // UI表示コルーチンを呼び出し、終わるまで待機
            yield return StartCoroutine(phaseUI.ShowPhaseAnnouncement(currentTurn, phaseName));
        }
        else
        {
            // phaseUIが設定されていなくてもゲームが止まらないように
            Debug.LogWarning("PhaseAnnouncementUIController が BattleManager に設定されていません。UI表示をスキップします。");
            yield return null; // 1フレームだけ待機
        }
    }

    /// <summary>
    /// 敵のターンが終了したときに呼び出される
    /// </summary>
    private void OnEnemyTurnFinished()
    {
        Debug.Log("【敵ターン終了】");
        currentTurn++;

        // カウンターが1回以上成功しているかチェック
        if (counterCount > 0)
        {
            Debug.Log($"カウンターが {counterCount} 回あります。エクストラターンに移行します。");
            // エクストラターン処理のコルーチンを開始
            StartCoroutine(HandleExtraTurnsAndContinue());
        }
        else
        {
            Debug.Log("カウンターはありません。通常の選択フェーズに移行します。");
            StartSelectionPhase();
        }
    }

    /// <summary>
    /// 攻撃対象を選択するフェーズを開始
    /// </summary>
    private void StartSelectionPhase()
    {
        Debug.Log("【攻撃対象選択ターン開始】");

        if (isFirstSelectionPhase)
        {
            // 最初のターンは必ず選択する
            isFirstSelectionPhase = false;
            StartCoroutine(ProcessSelectionPhase(keepSelections: false));
        }
        else
        {
            // 2ターン目以降（チュートリアル含む）：選択肢UIを表示する
            if (selectionChoicePanel != null)
            {
                selectionChoicePanel.SetActive(true);
                // 待機コルーチンを開始（ボタンが押されるのを待つ）
                selectionChoiceCoroutine = StartCoroutine(WaitForSelectionChoice());
            }
        }
    }

    /// <summary>
    /// 攻撃対象の選択が完了した時に呼び出される
    /// </summary>
    private void OnSelectionPhaseFinished()
    {
        if (currentPhase != null)
        {
            currentPhase.OnPhaseFinished -= OnSelectionPhaseFinished;
        }
        Debug.Log("【攻撃対象選択ターン終了】");
        if (isTutorialMode)
        {
            // チュートリアルモードの場合、次のチュートリアルフェーズ（カード選択）に移行
            Debug.Log("チュートリアル：カード選択フェーズに移行します。");

#if TUTORIAL_ENABLED
            // TutorialManager を初期化
            tutorialManager.Initialize(tortrialInputReader, enemyStatusUIs, enemyControllers);
            // currentPhase を TutorialManager に切り替え
            currentPhase = tutorialManager;
            // TutorialManager の終了イベントを購読
            currentPhase.OnPhaseFinished += OnCardTutorialPhaseFinished;
            if (tutorialUIPanel != null)
            {
                tutorialUIPanel.SetActive(true);
            }
            // 新しいフェーズを開始
            currentPhase.StartPhase();
#endif
        }
        else
        {
            // 通常モードの場合、プレイヤーのカード選択ターンを開始
            var playerSelections = selectTurn.PlayerSelections;
            playerTurn.Setup(playerSelections, battleCardDeck, enemyStatusUIs, enemyControllers);
            StartPlayerTurn();
        }
    }

    /// <summary>
    /// カード選択チュートリアル完了 → 敵ターンチュートリアルへ
    /// </summary>WQ
    private void OnCardTutorialPhaseFinished()
    {
        if (currentPhase != null)
        {
            currentPhase.OnPhaseFinished -= OnCardTutorialPhaseFinished;
        }

        Debug.Log("【カードチュートリアル完了】-> 敵ターンチュートリアルへ移行します");

        // UIを一旦非表示にする (TutorialManager が非表示にしなかった場合に備える)
        if (tutorialUIPanel != null)
        {
            tutorialUIPanel.SetActive(false);
        }

        currentPhase = enemyTurnTutorialManager;
        currentPhase.OnPhaseFinished += OnEnemyTurnTutorialFinished;

        // EnemyTurnTutorialManager のUIを表示する
        if (tutorialUIPanel != null)
        {
            tutorialUIPanel.SetActive(true);
        }

        currentPhase.StartPhase();
    }

    /// <summary>
    /// 敵ターンチュートリアル完了 → 通常の戦闘へ
    /// </summary>
    private void OnEnemyTurnTutorialFinished()
    {
        if (currentPhase != null)
        {
            currentPhase.OnPhaseFinished -= OnEnemyTurnTutorialFinished;
        }

        Debug.Log("【敵ターンチュートリアル完了】-> 通常戦闘へ移行します");

        isTutorialMode = false;
        playerTurn.SetTutorialMode(false);

        if (tutorialObjectsParent != null)
        {
            tutorialObjectsParent.SetActive(false);
        }

        // チュートリアル完了時にUIを非表示にする
        if (tutorialUIPanel != null)
        {
            tutorialUIPanel.SetActive(false);
        }

        InitializeNonTutorialPhases();

        StartSelectionPhase();
    }

    /// <summary>
    /// Playerターン開始時の処理
    /// </summary>
    private void StartPlayerTurn()
    {
        turnTime = playerTurnDuration; // Inspectorで設定した値を使用
        playerTurn.OnTurnFinished += OnPlayerTurnFinished;
        StartCoroutine(StartPlayerTurnWithTimer());
    }

    /// <summary>
    /// プレイヤーのターンをタイマー付きで開始
    /// </summary>
    public IEnumerator StartPlayerTurnWithTimer(string phaseName = "Player Phase")
    {
        yield return StartCoroutine(ShowPhaseUI(phaseName));
        Debug.Log("【カード選択ターン開始】");
        timeText.enabled = true;
        playerTurn.StartPlayerTurn();

        while (turnTime >= 0)
        {
            turnTime -= Time.deltaTime;
            timeText.text = turnTime.ToString("f2") + " <size=70%>SECOND</size>";
            yield return null;
        }
        playerTurn.FinishPlayerTurn();
    }

    /// <summary>
    /// プレイヤーターン終了 → 敵ターン
    /// </summary>
    private void OnPlayerTurnFinished()
    {
        Debug.Log("【カード選択ターン終了】");
        if (!isTutorialMode)
        {
            StartCoroutine(EnemyTurn());
        }
    }

    /// <summary>
    /// 敵のターン
    /// </summary>
    private IEnumerator EnemyTurn()
    {
        yield return StartCoroutine(ShowPhaseUI("Enemy Phase"));
        Debug.Log("【敵ターン開始】");
        enemyTurn.StartEnemyTurn();
        yield return null;
    }

    //=================================================================================
    // 選択フェーズのUIコールバック
    //=================================================================================

    /// <summary>
    /// 「優先順位を継続」ボタンが押されたときの処理
    /// </summary>
    public void OnKeepSelections()
    {
        Debug.Log("--- OnKeepSelections() が呼ばれました ---");
        if (selectionChoicePanel != null)
        {
            selectionChoicePanel.SetActive(false);
        }

        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        // 待機コルーチンを停止し、選択処理（継続）を開始
        if (selectionChoiceCoroutine != null)
        {
            StopCoroutine(selectionChoiceCoroutine);
            selectionChoiceCoroutine = null;
        }
        StartCoroutine(ProcessSelectionPhase(keepSelections: true));
    }

    /// <summary>
    /// 「優先順位を変更」ボタンが押されたときの処理
    /// </summary>
    public void OnChangeSelections()
    {
        Debug.Log("--- OnChangeSelections() が呼ばれました ---");
        if (selectionChoicePanel != null)
        {
            selectionChoicePanel.SetActive(false);
        }
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        // 待機コルーチンを停止し、選択処理（変更）を開始
        if (selectionChoiceCoroutine != null)
        {
            StopCoroutine(selectionChoiceCoroutine);
            selectionChoiceCoroutine = null;
        }
        StartCoroutine(ProcessSelectionPhase(keepSelections: false));
    }

    //=================================================================================
    // 選択フェーズの内部ロジック
    //=================================================================================

    /// <summary>
    /// 以前の選択がまだ有効か（選択した敵が生きているか）を検証する
    /// </summary>
    private bool ValidateKeptSelections(Dictionary<PlayerRuntime, List<EnemyModel>> selections)
    {
        Debug.Log("--- ValidateKeptSelections: 開始 ---");

        if (selections == null || selections.Count == 0)
        {
            Debug.LogWarning("ValidateKeptSelections: FAILED (selections 辞書が null または 0件です).");
            Debug.Log("--- ValidateKeptSelections: 終了 (false) ---");
            return false;
        }

        var livingEnemies = new HashSet<EnemyModel>(enemies.Where(e => e != null && e.EnemyHP > 0));
        if (livingEnemies.Count == 0)
        {
            Debug.LogWarning("ValidateKeptSelections: FAILED (生存している敵 (HP > 0) が 0 体です).");
            Debug.Log("--- ValidateKeptSelections: 終了 (false) ---");
            return false;
        }

        foreach (var playerSelectionList in selections.Values)
        {
            if (playerSelectionList == null || playerSelectionList.Count == 0)
            {
                // このプレイヤーは何も選択していない（これは「無効」ではない）
                continue;
            }

            //選択した敵が1体でも死んでいる（livingEnemiesセットにいない）かチェック
            foreach (var selectedEnemy in playerSelectionList)
            {
                if (selectedEnemy == null || !livingEnemies.Contains(selectedEnemy))
                {
                    // 選択した敵が null か、または livingEnemies に含まれていない（HPが0以下）
                    Debug.LogWarning($"ValidateKeptSelections: FAILED (プレイヤーが選択した敵 '{selectedEnemy?.EnemyName ?? "NULL"}' が生存していません).");
                    Debug.Log("--- ValidateKeptSelections: 終了 (false) ---");
                    return false; // 1体でも死んでいたら無効
                }
            }
        }

        Debug.Log("ValidateKeptSelections: PASSED (全ての選択が有効です。'継続'を許可します).");
        return true;
    }

    /// <summary>
    /// 継続/変更 の選択を待機するコルーチン
    /// </summary>
    private IEnumerator WaitForSelectionChoice()
    {
        // ボタンが押されるまで（OnKeep/OnChangeが呼ばれるまで）待機する
        Debug.Log("優先順位の選択（継続/変更）を待機中...");
        yield return null;
    }

    /// <summary>
    /// 選択フェーズの実行（継続/変更のロジックを含む）
    /// </summary>
    private IEnumerator ProcessSelectionPhase(bool keepSelections)
    {
        if (currentPhase == null)
        {
            Debug.LogError("currentPhase が設定されていません！");
            yield break;
        }

        bool isKeeping = false;

        if (keepSelections)
        {
            Debug.Log("優先順位を '継続' しました。");
            if (ValidateKeptSelections(selectTurn.PlayerSelections))
            {
                Debug.Log("選択は有効です。選択フェーズをスキップします。");
                isKeeping = true;
            }
            else
            {
                Debug.LogWarning("古い選択は無効です (倒された敵が含まれています)。選択をやり直してください。");
                selectTurn.ClearSelections(); // 選択をリセット
            }
        }
        else
        {
            Debug.Log("優先順位を '変更' しました。");
            selectTurn.ClearSelections();
        }

        if (!isKeeping || (isTutorialMode && isFirstSelectionPhase))
        {
            if (!isKeeping)
            {
                yield return StartCoroutine(ShowPhaseUI("Select Phase"));
            }
        }
        currentPhase.OnPhaseFinished += OnSelectionPhaseFinished;

        // currentPhase が SelectTurn かどうかを判定
        if (currentPhase is SelectTurn concreteSelectTurn)
        {
            // SelectTurn であれば、新しく作った StartPhase(bool) を呼ぶ
            concreteSelectTurn.StartPhase(isKeeping);
        }
        else
        {
            // チュートリアルなど、SelectTurn 以外の場合は、通常の StartPhase() を呼ぶ
            currentPhase.StartPhase();
        }
    }

    /// <summary>
    /// 渡されたPlayerRuntimeとTransformを基に、プレイヤーのプレハブとUIを生成・初期化する
    /// </summary>
    private void SpawnPlayerCharacter(PlayerRuntime runtime, Transform spawnPoint)
    {
        if (playerBasePrefab == null)
        {
            Debug.LogError("BattleManagerに 'Player Base Prefab' が設定されていません！", this);
            return; // Prefabがなければ処理中断
        }

        // Playerの「土台」を生成
        var playerObject = Instantiate(playerBasePrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
        PlayerController playerController = playerObject.GetComponent<PlayerController>();

        if (playerController == null)
        {
            Debug.LogError($"プレハブ '{playerBasePrefab.name}' のルートに PlayerController スクリプトがアタッチされていません！", playerObject);
            return;
        }

        playerController.Init(runtime.PlayerModel);
        runtime.PlayerController = playerController;
        playerControllers.Add(runtime.PlayerModel, playerController);

        // PlayerのStatusUI生成
        var statusUIObject = Instantiate(playerStatusUIPrefab, playerStatusBarTransform, false);
        PlayerStatusUIController playerUiController = statusUIObject.GetComponent<PlayerStatusUIController>();

        if (playerUiController == null)
        {
            Debug.LogError($"プレハブ '{playerStatusUIPrefab.name}' に PlayerStatusUIController スクリプトがアタッチされていません！", statusUIObject);
            return;
        }

        playerUiController.SetPlayerStatus(runtime);
        playerController.SetStatusUI(playerUiController);
        playerStatusUIs.Add(playerUiController);
    }

    public bool TrySpendGuardGauge(float amount)
    {
        if (currentGuardGauge >= amount)
        {
            currentGuardGauge -= amount;
            UpdateGuardGaugeUI();
            return true;
        }
        return false; // ゲージ不足
    }

    public void AddGuardGauge(float amount)
    {
        currentGuardGauge += amount;
        currentGuardGauge = Mathf.Clamp(currentGuardGauge, 0, MAX_GUARD_GAUGE);
        UpdateGuardGaugeUI();
    }

    private void UpdateGuardGaugeUI()
    {
        if (guardGaugeSlider != null)
        {
            guardGaugeSlider.value = currentGuardGauge / MAX_GUARD_GAUGE;
        }
    }

    // ---エクストラターン関連 ---

    public void IncrementCounterCount()
    {
        counterCount++;
    }

    private void OnExtraTurnFinished()
    {
        Debug.Log("【エクストラターン カード選択/攻撃 完了】");
        isExtraTurnSegmentFinished = true;
    }

    /// <summary>
    /// エクストラターンを実行し、その後通常の選択フェーズに移行する
    /// </summary>
    private IEnumerator HandleExtraTurnsAndContinue()
    {
        // 1. 通常の「プレイヤー⇒敵」の連携を一時的に解除
        playerTurn.OnTurnFinished -= OnPlayerTurnFinished;
        // 2. エクストラターン専用のハンドラを登録
        playerTurn.OnTurnFinished += OnExtraTurnFinished;

        // 3. カウンターカウント分だけループ
        while (counterCount > 0)
        {
            counterCount--;
            Debug.Log($"エクストラターン開始！ (残り: {counterCount})");

            isExtraTurnSegmentFinished = false;
            var playerSelections = selectTurn.PlayerSelections;
            playerTurn.Setup(playerSelections, battleCardDeck, enemyStatusUIs, enemyControllers);
            // プレイヤーのカード選択ターンを開始 (タイマー付き)
            StartCoroutine(StartPlayerTurnWithTimer());

            // エクストラターンが完了するまで待機
            yield return new WaitUntil(() => isExtraTurnSegmentFinished == true);
        }

        // 7. 全てのエクストラターンが終了したら、イベントハンドラを元に戻す
        playerTurn.OnTurnFinished -= OnExtraTurnFinished;
        playerTurn.OnTurnFinished += OnPlayerTurnFinished;

        // 8. 次の「通常の」選択フェーズを開始する
        StartSelectionPhase();
    }

    // <summary>
    /// 指定したプレイヤー(インデックス 0, 1, 2)の頭上にマーカーを表示する
    /// </summary>
    public void ShowTargetMarkerOnPlayer(int playerIndex)
    {
        if (markerInstance == null)
        {
            Debug.LogWarning("ターゲットマーカーのプレハブが設定されていません！");
            return;
        }
        if (playerIndex < 0 || playerIndex >= playerPositions.Count) return;

        // プレイヤーの「地面」の位置を取得
        Transform playerBase = playerPositions[playerIndex];

        // 地面から2ユニット上の位置にマーカーを移動
        markerInstance.transform.position = playerBase.position + new Vector3(0, 5, 0);
        markerInstance.transform.rotation = Quaternion.Euler(0, 0, 180);
        markerInstance.SetActive(true);
    }

    /// <summary>
    /// ターゲットマーカーを非表示にする
    /// </summary>
    public void HideTargetMarker()
    {
        if (markerInstance != null)
        {
            markerInstance.SetActive(false);
        }
    }
    /// <summary>
    /// 防御結果のテキスト (GUARD, COUNTER, HIT) を表示する
    /// </summary>
    public void ShowDefenseFeedback(string message, Color color)
    {
        if (defenseFeedbackText == null) return;

        // 既存の表示コルーチンが動いていたら停止 (連続で表示する場合)
        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
        }

        // 新しいコルーチンを開始
        feedbackCoroutine = StartCoroutine(ShowFeedbackCoroutine(message, color));
    }

    /// <summary>
    /// テキストを指定時間表示して非表示にするコルーチン
    /// </summary>
    private IEnumerator ShowFeedbackCoroutine(string message, Color color)
    {
        defenseFeedbackText.text = message;
        defenseFeedbackText.color = color;
        defenseFeedbackText.gameObject.SetActive(true);

        // (ここでフェードインなどのアニメーションを入れても良い)

        yield return new WaitForSeconds(feedbackDisplayDuration);

        // (ここでフェードアウトなどのアニメーションを入れても良い)

        defenseFeedbackText.gameObject.SetActive(false);
        feedbackCoroutine = null;
    }
}
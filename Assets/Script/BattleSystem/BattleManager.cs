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
    [SerializeField] private Text guardCounterText;

    [Header("ゲーム内データ")]
    [SerializeField] private List<StageEnemyData> allStageEnemyData;

#if TUTORIAL_ENABLED
    [Header("チュートリアル用コンポーネント")]
    [SerializeField] private GameObject tutorialObjectsParent; // チュートリアル関連オブジェクトの親
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private SelectTurnTutorialManager selectTurnTutorialManager;
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

    private EnemyModel enemyModel;
    private StageEnemyData currentStage;
    private EnemyStatusUIController enemyUIController;
    private float turnTime = 10f;
    private bool isTutorialMode = false;
    private IPhase currentPhase;
    private bool isFirstSelectionPhase = true; // 最初の選択フェーズかどうかを判定するフラグ
    private Coroutine selectionChoiceCoroutine; // 選択待機コルーチンを保持する変数

    private float currentGuardGauge;
    private int counterCount = 0;
    private bool isExtraTurnSegmentFinished = false; // エクストラターン用フラグ
    private const float MAX_GUARD_GAUGE = 100f;
    //=================================================================================
    // ライフサイクル (Startを分割)
    //=================================================================================

    void Start()
    {
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
            selectTurnTutorialManager.Initialize(tortrialInputReader, players, enemies, playerStatusUIs, enemyStatusUIs);
            currentPhase = selectTurnTutorialManager;
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
        enemyTurn.EnemySetup(playerModels, enemies, playerStatusUIs);
        enemyTurn.TurnFinished += OnEnemyTurnFinished;
    }

    /// <summary>
    /// 通常モード（非チュートリアル）のフェーズ初期化
    /// </summary>
    private void InitializeNonTutorialPhases()
    {
        selectTurn.Initialize(players, enemies, playerStatusUIs, enemyStatusUIs);
        currentPhase = selectTurn;
    }

    //=================================================================================
    // キャラクターとステージのセットアップ
    //=================================================================================

    private void playerView()
    {
        if (isTutorialMode)
        {
            // --- チュートリアルモードの処理 ---
            Debug.Log("チュートリアルモード：Player ID 1 を検索・生成します...");

            // 1. パーティリスト (players) から PlayerID が 1 のキャラクターを探す
            PlayerRuntime tutorialPlayer = players.FirstOrDefault(p => p.PlayerModel.PlayerID == 1); // 0 から 1 に変更

            if (tutorialPlayer != null)
            {
                // 2. プレイヤーが見つかった場合、ポジション[0] (最初の出現位置) に生成
                if (playerPositions.Count > 0 && playerPositions[0] != null)
                {
                    Transform spawnPoint = playerPositions[0];
                    // ヘルパーメソッドを呼び出し
                    SpawnPlayerCharacter(tutorialPlayer, spawnPoint);
                }
                else
                {
                    Debug.LogError("チュートリアル用のプレイヤー(ID:1)は見つかりましたが、playerPositions[0]が設定されていません！"); // ログを ID:1 に
                }
            }
            else
            {
                Debug.LogError("チュートリアルモードですが、パーティ内に Player ID 1 のプレイヤーが見つかりませんでした！"); // ログを ID:1 に
            }
        }
        else
        {
            // --- 通常モードの処理 (以前と同じ) ---
            // パーティ全員を順番に生成
            for (int i = 0; i < players.Count; i++)
            {
                if (i >= playerPositions.Count || playerPositions[i] == null)
                {
                    Debug.LogWarning($"Player {i} のためのポジションが定義されていないか、None(Transform)です。このプレイヤーの生成をスキップします。");
                    continue; // 'break' ではなく 'continue' に変更
                }

                PlayerRuntime runtime = players[i];
                Transform spawnPoint = playerPositions[i];

                // ヘルパーメソッドを呼び出し
                SpawnPlayerCharacter(runtime, spawnPoint);
            }
        }
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
            enemyController.Init(enemy);

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
    /// 敵のターンが終了したときに呼び出される
    /// </summary>
    private void OnEnemyTurnFinished()
    {
        Debug.Log("【敵ターン終了】");
        StartSelectionPhase(); // ここで次の選択フェーズを開始する
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
            tutorialManager.Initialize(tortrialInputReader, enemyStatusUIs);

            // currentPhase を TutorialManager に切り替え
            currentPhase = tutorialManager;

            // TutorialManager の終了イベントを購読
            currentPhase.OnPhaseFinished += OnCardTutorialPhaseFinished;

            // 新しいフェーズを開始
            currentPhase.StartPhase();
#endif
        }
        else
        {
            // 通常モードの場合、プレイヤーのカード選択ターンを開始
            var playerSelections = selectTurn.PlayerSelections;
            playerTurn.Setup(playerSelections, battleCardDeck, enemyStatusUIs);
            StartPlayerTurn();
        }
    }

    private void OnCardTutorialPhaseFinished()
    {
        if (currentPhase != null)
        {
            currentPhase.OnPhaseFinished -= OnCardTutorialPhaseFinished;
        }

        Debug.Log("【チュートリアルバトル完了】");

        // チュートリアルオブジェクトを非表示にする
        if (tutorialObjectsParent != null)
        {
            tutorialObjectsParent.SetActive(false);
        }
#if TUTORIAL_ENABLED
        else
        {
            selectTurnTutorialManager.gameObject.SetActive(false);
            tutorialManager.gameObject.SetActive(false);
        }
#endif
        // TODO: ここでリザルト画面に遷移したり、メインメニューに戻る処理を呼び出す
        // 例: SceneManager.LoadScene("MainMenu");
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
    public IEnumerator StartPlayerTurnWithTimer()
    {
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
        if (guardCounterText != null)
        {
            // (現在のゲージ量 / 1回のコスト) の小数点以下を切り捨て
            int possibleCounters = Mathf.FloorToInt(currentGuardGauge / GUARD_COST);

            // Textコンポーネントに数字（"3", "2", "1", "0"など）を設定
            guardCounterText.text = possibleCounters.ToString();
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
            playerTurn.Setup(playerSelections, battleCardDeck, enemyStatusUIs);

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
}
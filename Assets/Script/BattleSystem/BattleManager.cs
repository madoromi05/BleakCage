using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Battleの流れを管理するクラス
/// </summary>
public class BattleManager : MonoBehaviour
{
    public TextMeshProUGUI timeText;
    public bool isTurnFinished;

    [SerializeField] private PlayerTurn playerTurn;
    [SerializeField] private EnemyTurn enemyTurn;
    [SerializeField] private SelectTurn selectTurn;
    [SerializeField] public BattleCardDeck battleDeck;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform partyTextureTransform;
    [SerializeField] private Transform enemyTextureTransform;
    [SerializeField] private List<StageEnemyData> allStageEnemyData;

#if TUTORIAL_ENABLED
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private TortrialInputReader tutorialInputReader; // TutorialManagerで使用
#endif

    private PlayerModelFactory playerModelFactory = new PlayerModelFactory();
    private List<PlayerRuntime> party = new List<PlayerRuntime>();
    private List<EnemyModel> predators = new List<EnemyModel>();
    private List<PlayerStatusUIController> playerUIs = new List<PlayerStatusUIController>();
    private List<EnemyStatusUIController> enemyUIs = new List<EnemyStatusUIController>();

    private float turnTime = 10f; // プレイヤーのターン時間（秒）
    private int turnCount;
    private StageEnemyData currentStage;

    private List<EnemyModel> player1Select = new List<EnemyModel>();
    private List<EnemyModel> player2Select = new List<EnemyModel>();
    private List<EnemyModel> player3Select = new List<EnemyModel>();

    public event System.Action BattleFinished;                    // バトル終了イベント
    void Start()
    {
        // 1. パーティーとカードデータを読み込み
        turnCount = 1;
        var dataLoader = new PlayerDataLoader();
        DeckSetupRepository setupData = dataLoader.LoadPlayerPartyAndCards();
        this.party = setupData.Party;

        // 2.PlayerViewとEnemyViewを生成
        partyPlayerView();
        getStageID();

        enemiesCreate();

        List<PlayerModel> playerModels = party.Select(p => p.PlayerModel).ToList();
        enemyTurn.EnemySetup(playerModels, predators);

        // 4. バトルデッキとプレイヤーのターンをセットアップ
        battleDeck.InitFromCardList(setupData.AllCards);
        playerTurn.OnTurnFinished += OnPlayerTurnFinished;
        enemyTurn.TurnFinished += OnEnemyTurnFinished;
        selectTurn.SelectTurnFinished += OnSelectTurnFinished;
        BattleFinished += OnBattleFinished;

        // 5. バトル開始
#if TUTORIAL_ENABLED
        // チュートリアルマネージャーに参照を渡して、チュートリアルフローを開始
        tutorialManager.StartTutorialFlow(this, playerTurn, enemyTurn, tutorialInputReader);
#else
        // チュートリアルが無効な場合は、直接バトルを開始
        StartCoroutine(BattleFlow());
#endif
    }

    public void StartBattleAfterTutorial()
    {
        StartCoroutine(BattleFlow());
    }

    private void OnDisable()
    {
        // --- 追加：オブジェクトが無効になった際にイベントの購読を解除 ---
        playerTurn.OnTurnFinished -= OnPlayerTurnFinished;
        enemyTurn.TurnFinished -= OnEnemyTurnFinished;
        selectTurn.SelectTurnFinished -= OnSelectTurnFinished;
        BattleFinished -= OnBattleFinished;
    }
    private void partyPlayerView()
    {
        for (int i = 0; i < party.Count; i++)
        {
            var playerObject = Instantiate(playerPrefab, partyTextureTransform, false);
            PlayerController playerController = playerObject.GetComponent<PlayerController>();
            playerController.Init(party[i].PlayerModel);
            PlayerStatusUIController uiController = playerObject.GetComponent<PlayerStatusUIController>();
        }
    }

    void getStageID()
    {
        int currentStageID = StageManager.SelectedStageID;

        if (currentStageID == -1)
        {
            Debug.LogError("ステージIDが設定されていません！StageManager.SelectedStageIDを確認してください。");
#if UNITY_EDITOR
            string sceneName = SceneManager.GetActiveScene().name;
            // ※チュートリアルシーン名が "Tutorial" でない場合は、実際のシーン名に変更してください
            if (sceneName == "Tutorial")
            {
                currentStageID = 0;
                Debug.Log($"シーン名'{sceneName}'のため、ステージIDを '0' (チュートリアル)に設定しました。");
            }
            else
            {
                currentStageID = 1;
                Debug.Log($"シーン名'{sceneName}'のため、ステージIDを '1' (通常戦闘)に設定しました。");
            }
#endif
        }
        currentStage = allStageEnemyData.FirstOrDefault(stage => stage.stageEnemyID == currentStageID);
    }
    private void enemiesCreate()
    {
        // ステージデータがnullの場合はエラーを出し、処理を中断
        if (currentStage == null || currentStage.enemyIDs == null)
        {
            Debug.LogError("敵の生成に失敗しました。ステージデータが正しく設定されていません。");
            return;
        }

        var enemyFactory = new EnemyModelFactory();
        foreach (int enemyId in currentStage.enemyIDs)
        {
            EnemyModel enemy = enemyFactory.CreateFromId(enemyId);
            predators.Add(enemy);

            var enemyObject = Instantiate(enemyPrefab, enemyTextureTransform, false);
            EnemyController enemyController = enemyObject.GetComponent<EnemyController>();
            enemyController.Init(enemy);

            EnemyStatusUIController uiController = enemyObject.GetComponent<EnemyStatusUIController>();
            if (uiController != null)
            {
                uiController.SetEnemyStatus(enemy);
                enemyUIs.Add(uiController);
            }
        }
    }

    private IEnumerator BattleFlow()
    {
        while (true) // ゲーム終了までループ
        {
            // 1. 選択ターン
            Debug.Log($"【ターン{turnCount}: 攻撃優先順位選択 開始】");
            isTurnFinished = false;
            selectTurn.StartSelectTurn(party, predators, playerUIs, enemyUIs);
            yield return new WaitUntil(() => isTurnFinished);
            Debug.Log("【攻撃優先順位選択 終了】");

            playerTurn.Setup(selectTurn.PlayerSelections, battleDeck);

            // 2. プレイヤーの攻撃ターン
            Debug.Log("【プレイヤーターン開始】");
            isTurnFinished = false;
            playerTurn.StartPlayerTurn();

            float currentTurnTime = turnTime;
            while (currentTurnTime > 0 && !isTurnFinished)
            {
                currentTurnTime -= Time.deltaTime;
                timeText.text = turnTime.ToString("f2") + " <size=70%>SECOND</size>";
                yield return null;
            }
            // 終了タイミングが異なるときのために表記を0にする
            timeText.text = "0.00 <size=70%>SECOND</size>";

            // 時間切れか、ターンが外部から終了された
            if (!isTurnFinished)
            {
                playerTurn.FinishPlayerTurn(); // 時間切れでターン終了
            }
            yield return new WaitUntil(() => isTurnFinished); // ターン終了処理の完了を待つ

            // --- 3. 敵のターン ---
            Debug.Log("【敵ターン開始】");
            isTurnFinished = false;
            enemyTurn.StartEnemyTurn();
            yield return new WaitUntil(() => isTurnFinished);

            // バトル終了条件をチェック
            if (CheckBattleEndCondition()) yield break;

            // --- 4. 次のターンへ ---
            turnCount++;
        }
    }

    private void OnSelectTurnFinished()
    {
        Debug.Log("選択ターン終了処理 完了");
        isTurnFinished = true; // BattleFlowに完了を通知
    }

    private void OnPlayerTurnFinished()
    {
        Debug.Log("【プレイヤーターン終了】");
        isTurnFinished = true; // BattleFlowに完了を通知
    }

    private void OnEnemyTurnFinished()
    {
        Debug.Log("【敵ターン終了】");
        isTurnFinished = true; // BattleFlowに完了を通知
    }

    private bool CheckBattleEndCondition()
    {
        if (party.All(p => p.CurrentHP <= 0) || predators.All(e => e.EnemyHP <= 0))
        {
            BattleFinished?.Invoke();
            return true;
        }
        return false;
    }

    private void OnBattleFinished()
    {
        Debug.Log("戦闘終了！");
        timeText.text = "";
        // ここにリザルト画面への遷移などの処理を記述
        StopAllCoroutines(); // 全てのコルーチンを停止
    }

    /// <summary>
    /// プレイヤーのターンを10秒間開始
    /// </summary>
    public IEnumerator StartPlayerTurnWithTimer()
    {
        Debug.Log("【プレイヤーターン開始】");
        isTurnFinished = false;
        playerTurn.StartPlayerTurn(); // タイマーなしでカード選択を開始

        float currentTurnTime = turnTime;
        while (currentTurnTime > 0 && !isTurnFinished)
        {
            currentTurnTime -= Time.deltaTime;
            timeText.text = currentTurnTime.ToString("f2") + " <size=70%>SECOND</size>";
            yield return null;
        }

        if (!isTurnFinished)
        {
            playerTurn.FinishPlayerTurn();
        }
        yield return new WaitUntil(() => isTurnFinished);
    }
}
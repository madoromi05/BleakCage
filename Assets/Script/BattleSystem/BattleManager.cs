using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Battleのターン、順番とデータ管理
/// </summary>
public class BattleManager : MonoBehaviour
{
    [SerializeField] public BattleCardDeck battleCardDeck;
    [SerializeField] private PlayerTurn playerTurn;
    [SerializeField] private EnemyTurn enemyTurn;
    [SerializeField] private SelectTurn selectTurn;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform playerParent;
    [SerializeField] private Transform enemyParent;
    [SerializeField] private List<Transform> playerPositions;
    [SerializeField] private List<Transform> enemyPositions;
    [SerializeField] private Transform playerStatusBarTransform;
    [SerializeField] private Transform enemyStatusBarTransform;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private List<StageEnemyData> allStageEnemyData;
    [SerializeField] private GameObject playerStatusUIPrefab;
    [SerializeField] private GameObject enemyStatusUIPrefab;

#if TUTORIAL_ENABLED
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private TortrialInputReader tortrialInputReader;
#endif

    private List<PlayerRuntime> playerParty;
    private List<EnemyModel> predators = new List<EnemyModel>();

    private List<PlayerStatusUIController> playerStatusUIs = new List<PlayerStatusUIController>();
    private List<EnemyStatusUIController> enemyStatusUIs = new List<EnemyStatusUIController>();

    private EnemyModel enemyModel;
    private StageEnemyData currentStage;
    private EnemyStatusUIController enemyUIController;
    private float turnTime = 10f;
    private bool isTutorialMode = false;

    void Start()
    {
        // Playerデータのロード
        var dataLoader = new PlayerDataLoader();
        DeckSetupRepository setupData = dataLoader.LoadPlayerPartyAndCards();
        playerParty = setupData.Party;
        List<CardRuntime> allCardsForDeck = setupData.AllCards;
        // 読み込んだカードがない場合はエラーログを出して停止
        if (allCardsForDeck == null || allCardsForDeck.Count == 0)
        {
            Debug.LogError("デッキにセットするカードが1枚もありません！ PlayerDataLoader の処理を確認してください。");
            return;
        }
        battleCardDeck.InitFromCardList(allCardsForDeck);

        playerView();
        stageSet();
        enemyView();

        List<PlayerModel> playerModels = playerParty.Select(p => p.PlayerModel).ToList();
        enemyTurn.EnemySetup(playerModels, predators);
        enemyTurn.TurnFinished += OnEnemyTurnFinished;

        //敵のIDが0の場合tuterealを開始する
        if (predators[0].EnemyID == 0)
        {
#if TUTORIAL_ENABLED
            isTutorialMode = true;
            tutorialManager.StartTutorialFlow(this, playerTurn, enemyTurn, tortrialInputReader);
#endif
        }
            StartSelectionPhase();
    }

    private void playerView()
    {
        // パーティ人数分 PlayerView を生成
        for (int i = 0; i < playerParty.Count; i++)
        {
            if (i >= playerPositions.Count)
            {
                Debug.LogError($"Player {i} のためのポジションが定義されていません。");
                break;
            }

            PlayerRuntime runtime = playerParty[i];

            // PlayerのModel生成
            Transform spawnPoint = playerPositions[i];
            var playerObject = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
            PlayerController playerController = playerObject.GetComponent<PlayerController>();
            playerController.Init(runtime.PlayerModel);
            runtime.PlayerController = playerController;

            // PlayerのStatusUI生成
            var statusUIObject = Instantiate(playerStatusUIPrefab, playerStatusBarTransform, false);
            PlayerStatusUIController playerUiController = statusUIObject.GetComponent<PlayerStatusUIController>();
            playerUiController.SetPlayerStatus(runtime);
            playerController.SetStatusUI(playerUiController);
            playerStatusUIs.Add(playerUiController);
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
            predators.Add(enemy);

            // 指定された3D座標に敵を生成
            Transform spawnPoint = enemyPositions[i];
            var enemyObject = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
            EnemyController enemyController = enemyObject.GetComponent<EnemyController>();
            enemyController.Init(enemy);

            // 敵のStatusUI生成
            var statusUIObject = Instantiate(enemyStatusUIPrefab, enemyStatusBarTransform, false);
            EnemyStatusUIController uiController = statusUIObject.GetComponent<EnemyStatusUIController>();
            uiController.SetEnemyStatus(enemy);
            enemyController.SetStatusUI(uiController);
            enemyStatusUIs.Add(uiController);
        }
    }

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
        selectTurn.SelectTurnFinished += OnSelectionPhaseFinished;
        selectTurn.StartSelectTurn(playerParty, predators, playerStatusUIs, enemyStatusUIs);
    }

    /// <summary>
    /// 攻撃対象の選択が完了した時に呼び出される
    /// </summary>
    private void OnSelectionPhaseFinished()
    {
        selectTurn.SelectTurnFinished -= OnSelectionPhaseFinished;
        Debug.Log("【攻撃対象選択ターン終了】");

        // SelectTurnから選択結果を取得
        var playerSelections = selectTurn.PlayerSelections;

        // PlayerTurnを選択されたターゲット情報でセットアップ
        var targetEnemyUI = enemyStatusUIs.FirstOrDefault();
         playerTurn.Setup(playerSelections, battleCardDeck, enemyStatusUIs);

        // プレイヤーのカード選択ターンを開始
        StartPlayerTurn();
    }

    /// <summary>
    /// Playerターン開始時の処理
    /// </summary>
    private void StartPlayerTurn()
    {
        turnTime = 10f;
        playerTurn.OnTurnFinished += OnPlayerTurnFinished;
        StartCoroutine(StartPlayerTurnWithTimer());
    }

    /// <summary>
    /// プレイヤーのターンを10秒間開始
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
    /// プレイヤーターン終了 → 敵ターン → プレイヤーターン再開
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
    /// 敵のターン（仮実装）→ すぐにプレイヤーターン再開
    /// </summary>
    private IEnumerator EnemyTurn()
    {
        Debug.Log("【敵ターン開始】");
        enemyTurn.StartEnemyTurn();
        yield return null;
    }

    /////////////////////////////////////////////////////////////////////////////////////////
    ///チュートリアル用の処理
    /////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// チュートリアル用にタイマーを停止させる
    /// </summary>
    public void StopTurnTimer()
    {
        StopCoroutine("StartPlayerTurnWithTimer"); // Coroutineを停止
    }

    /// <summary>
    /// チュートリアル用にプレイヤーのターンを開始する（タイマーなし）
    /// </summary>
    public void StartPlayerTurnForTutorial()
    {
        Debug.Log("【プレイヤーターン開始 (チュートリアル)】");
        playerTurn.StartPlayerTurn();
    }
}
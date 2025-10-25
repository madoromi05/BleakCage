using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private List<Transform> playerPositions;
    [SerializeField] private List<Transform> enemyPositions;
    [SerializeField] private GameObject playerStatusUIPrefab;
    [SerializeField] private GameObject enemyStatusUIPrefab;

    [Header("UI関連")]
    [SerializeField] private Transform playerStatusBarTransform;
    [SerializeField] private Transform enemyStatusBarTransform;
    [SerializeField] private Text timeText;

    [Header("ゲーム内データ")]
    [SerializeField] private List<StageEnemyData> allStageEnemyData;

#if TUTORIAL_ENABLED
    [Header("チュートリアル用コンポーネント")]
    [SerializeField] private GameObject tutorialObjectsParent; // [修正] チュートリアル関連オブジェクトの親
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private SelectTurnTutorialManager selectTurnTutorialManager;
    [SerializeField] private TutorialInputReader tortrialInputReader;
#endif
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

    void Start()
    {
        // Playerデータのロード
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

        playerView();
        stageSet();
        enemyView();

        //チュートリアルモードの判定
        isTutorialMode = enemies.Count > 0 && enemies[0].EnemyID == 0;

        // チュートリアルモードの場合、関連イベントを購読
        if (isTutorialMode)
        {
            // [修正] チュートリアル用の親オブジェクトを有効化 (すべての子を含む)
            if (tutorialObjectsParent != null)
            {
                tutorialObjectsParent.SetActive(true);
            }
            else
            {
                // 親が設定されていない場合は、個別に有効化
                selectTurnTutorialManager.gameObject.SetActive(true);
                tutorialManager.gameObject.SetActive(true);
            }

            selectTurnTutorialManager.Initialize(tortrialInputReader, players, enemies, playerStatusUIs, enemyStatusUIs);
            currentPhase = selectTurnTutorialManager;
        }
        else
        {
            if (tutorialObjectsParent != null)
            {
                tutorialObjectsParent.SetActive(false);
            }
            else
            {
                selectTurnTutorialManager.gameObject.SetActive(false);
                tutorialManager.gameObject.SetActive(false);
            }

            selectTurn.Initialize(players, enemies, playerStatusUIs, enemyStatusUIs);
            currentPhase = selectTurn;
        }

        currentPhase.OnPhaseFinished += OnSelectionPhaseFinished;

        List<PlayerModel> playerModels = players.Select(p => p.PlayerModel).ToList();
        enemyTurn.EnemySetup(playerModels, enemies, playerStatusUIs);
        enemyTurn.TurnFinished += OnEnemyTurnFinished;

        StartSelectionPhase();
    }

    private void playerView()
    {
        // パーティ人数分 PlayerView を生成
        for (int i = 0; i < players.Count; i++)
        {
            if (i >= playerPositions.Count)
            {
                Debug.LogError($"Player {i} のためのポジションが定義されていません。");
                break;
            }

            PlayerRuntime runtime = players[i];

            if (playerPrefab == null)
            {
                Debug.LogError("BattleManagerのインスペクターで 'Player Prefab' が設定されていません！ (None)");
                break; // ループ中断
            }
            // PlayerのModel生成
            Transform spawnPoint = playerPositions[i];
            var playerObject = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
            PlayerController playerController = playerObject.GetComponent<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError($"プレハブ '{playerPrefab.name}' のルートに PlayerController スクリプトがアタッチされていません！", playerObject);
                continue; // 次のプレイヤーの処理へ
            }
            playerController.Init(runtime.PlayerModel);
            runtime.PlayerController = playerController;

            // PlayerのStatusUI生成
            var statusUIObject = Instantiate(playerStatusUIPrefab, playerStatusBarTransform, false);
            PlayerStatusUIController playerUiController = statusUIObject.GetComponent<PlayerStatusUIController>();
            if (playerUiController == null)
            {
                Debug.LogError($"プレハブ '{playerStatusUIPrefab.name}' に PlayerStatusUIController スクリプトがアタッチされていません！", statusUIObject);
                continue;
            }
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
            if (sceneName == "Tutorial" || sceneName == "Battle_Tutorial") // [修正] シーン名 "Battle_Tutorial" も考慮
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
            enemies.Add(enemy);

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

        if (currentPhase != null)
        {
            currentPhase.StartPhase();
        }
        else
        {
            Debug.LogError("currentPhase が設定されていません！");
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

        // [修正] チュートリアルモードかどうかで処理を分岐
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

    // [追加] カード選択チュートリアル (TutorialManager) が終了したときに呼び出される
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
}
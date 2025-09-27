using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.Overlays;
using UnityEngine;

/// <summary>
/// Battleのターン、順番とデータ管理
/// </summary>
public class BattleManager : MonoBehaviour
{
    [SerializeField] private PlayerTurn playerTurn;
    [SerializeField] private EnemyTurn enemyTurn;
    [SerializeField] private BattleCardDeck battleDeck;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform partyTextureTransform;
    [SerializeField] private Transform enemyTextureTransform;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private List<StageEnemyData> allStageEnemyData;

#if TUTORIAL_ENABLED
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private TortrialInputReader tortrialInputReader;
#endif
    private List<PlayerRuntime> party = new List<PlayerRuntime>();
    private List<EnemyModel> predators = new List<EnemyModel>();

    private bool isTutorialMode = false;
    private EnemyModel enemyModel;
    private float turnTime = 10f; // プレイヤーのターン時間（秒）

    void Start()
    {
        var dataLoader = new PlayerDataLoader();
        DeckSetupRepository setupData = dataLoader.LoadPlayerPartyAndCards();
        this.party = setupData.Party;

        // パーティ人数分 PlayerView を生成
        for (int i = 0; i < party.Count; i++)
        {
            PlayerRuntime runtime = party[i];

            var playerObject = Instantiate(playerPrefab, partyTextureTransform, false);

            PlayerController playerController = playerObject.GetComponent<PlayerController>();
            playerController.Init(runtime.PlayerModel);
            runtime.PlayerController = playerController;
        }

        // 3. 敵を生成
        int currentStageID = StageManager.SelectedStageID; // 別途作成するクラスから選択されたステージIDを取得
        StageEnemyData currentStage = allStageEnemyData.FirstOrDefault(stage => stage.stageEnemyID == currentStageID);

        if (currentStage == null)
        {
            Debug.LogError($"ステージID {currentStageID} のデータが見つかりません！");
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
            Debug.Log($"敵(ID: {enemy.EnemyID}) を生成しました。");
        }

        //最初の敵をターゲットとして設定する
        if (predators.Count > 0)
        {
            enemyModel = predators[0];
        }
        else
        {
            Debug.LogError("攻撃対象の敵が見つかりません！");
            return;
        }

        List<PlayerModel> playerModels = party.Select(p => p.PlayerModel).ToList();
        enemyTurn.EnemySetup(playerModels, predators);

        battleDeck.InitFromCardList(setupData.AllCards);
        playerTurn.Setup(party[0], enemyModel, battleDeck);

        //敵のIDが0の場合tuterealを開始する
        if (predators[0].EnemyID == 0)
        {
#if TUTORIAL_ENABLED
            isTutorialMode = true;
            tutorialManager.StartTutorialFlow(this, playerTurn, enemyTurn, tortrialInputReader);
#else
            // チュートリアルが無効化されている場合、代わりに通常のプレイヤーターンを開始する
            Debug.LogWarning("敵IDが0ですが、チュートリアルは無効です。通常どおりゲームを開始します。");
            StartPlayerTurn();
#endif
        }
        else
        {
            StartPlayerTurn();
        }
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
        Debug.Log("【プレイヤーターン開始】");
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
        Debug.Log("【プレイヤーターン終了】");
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

        yield return new WaitForSeconds(1.0f);

        Debug.Log("【敵ターン終了】");

        StartPlayerTurn();
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
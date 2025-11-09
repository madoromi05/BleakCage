using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

/// <summary>
/// バトル開始時のエンティティ（プレイヤー、敵）の生成、配置、参照管理を担う
/// </summary>
public class BattleEntitiesManager : MonoBehaviour
{
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

    [Header("ゲーム内データ")]
    [SerializeField] private List<StageEnemyData> allStageEnemyData;

    public List<PlayerRuntime> Players { get; private set; } = new List<PlayerRuntime>();
    public List<EnemyModel> Enemies { get; private set; } = new List<EnemyModel>();
    public List<PlayerStatusUIController> PlayerStatusUIs { get; private set; } = new List<PlayerStatusUIController>();
    public List<EnemyStatusUIController> EnemyStatusUIs { get; private set; } = new List<EnemyStatusUIController>();
    public Dictionary<PlayerModel, PlayerController> PlayerControllers { get; private set; } = new Dictionary<PlayerModel, PlayerController>();
    public Dictionary<EnemyModel, EnemyController> EnemyControllers { get; private set; } = new Dictionary<EnemyModel, EnemyController>();
    public StageEnemyData CurrentStage { get; private set; }

    public bool IsTutorialMode { get; private set; } = false;

    public void Setup()
    {
        LoadGameData();
        SetupStageAndCharacters();
        // チュートリアルモードの判定
        IsTutorialMode = Enemies.Count > 0 && Enemies[0].EnemyID == 0;
    }

    /// <summary>
    /// プレイヤーデータとカードデッキをロード・初期化
    /// </summary>
    private void LoadGameData()
    {
        var dataLoader = new PlayerDataLoader();
        DeckSetupRepository setupData = dataLoader.LoadPlayerPartyAndCards();
        Players = setupData.Party;
    }

    /// <summary>
    /// ステージデータをロードし、プレイヤーと敵を生成・配置
    /// </summary>
    private void SetupStageAndCharacters()
    {
        stageSet();
        enemyView();
        playerView();
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
        CurrentStage = allStageEnemyData.FirstOrDefault(stage => stage.stageEnemyID == currentStageID);

        if (CurrentStage == null)
        {
            Debug.LogError($"ID {currentStageID} に一致する StageEnemyData が見つかりません！");
            return;
        }
    }

    private void enemyView()
    {
        var enemyFactory = new EnemyModelFactory();
        for (int i = 0; i < CurrentStage.enemyIDs.Count; i++)
        {
            if (i >= enemyPositions.Count) break;

            int enemyId = CurrentStage.enemyIDs[i];
            EnemyModel enemy = enemyFactory.CreateFromId(enemyId);
            Enemies.Add(enemy);

            Transform spawnPoint = enemyPositions[i];
            var enemyObject = Instantiate(enemyBasePrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
            EnemyController enemyController = enemyObject.GetComponent<EnemyController>();

            if (playerPositions.Count > 0 && playerPositions[0] != null)
            {
                enemyController.Init(enemy, playerPositions[0]);
            }

            EnemyControllers.Add(enemy, enemyController);

            // 敵のStatusUI生成
            var statusUIObject = Instantiate(enemyStatusUIPrefab, enemyStatusBarTransform, false);
            EnemyStatusUIController uiController = statusUIObject.GetComponent<EnemyStatusUIController>();
            uiController.SetEnemyStatus(enemy);
            enemyController.SetStatusUI(uiController);
            EnemyStatusUIs.Add(uiController);
        }
    }

    private void playerView()
    {
        // プレイヤーを1体だけ生成するロジック
        PlayerRuntime targetPlayer = Players.FirstOrDefault(p => p.PlayerModel.PlayerID == 1) ?? Players.FirstOrDefault();

        if (targetPlayer == null || playerPositions.Count == 0 || playerPositions[0] == null)
        {
            Debug.LogError("プレイヤーデータまたは出現位置が設定されていません！");
            return;
        }

        Transform spawnPoint = playerPositions[0];
        SpawnPlayerCharacter(targetPlayer, spawnPoint);
    }

    private void SpawnPlayerCharacter(PlayerRuntime runtime, Transform spawnPoint)
    {
        var playerObject = Instantiate(playerBasePrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
        PlayerController playerController = playerObject.GetComponent<PlayerController>();
        if (playerController == null) return;

        playerController.Init(runtime.PlayerModel);
        runtime.PlayerController = playerController;
        PlayerControllers.Add(runtime.PlayerModel, playerController);

        // PlayerのStatusUI生成
        var statusUIObject = Instantiate(playerStatusUIPrefab, playerStatusBarTransform, false);
        PlayerStatusUIController playerUiController = statusUIObject.GetComponent<PlayerStatusUIController>();
        if (playerUiController == null) return;

        playerUiController.SetPlayerStatus(runtime);
        playerController.SetStatusUI(playerUiController);
        PlayerStatusUIs.Add(playerUiController);
    }

    /// <summary>
    /// 指定したプレイヤーの頭上にマーカーを表示する
    /// </summary>
    public void ShowTargetMarkerOnPlayer(GameObject markerInstance, int playerIndex)
    {
        if (markerInstance == null || playerIndex < 0 || playerIndex >= playerPositions.Count) return;

        Transform playerBase = playerPositions[playerIndex];

        // マーカーを適切な位置に移動・表示
        markerInstance.transform.position = playerBase.position + new Vector3(0, 5, 0);
        markerInstance.transform.rotation = Quaternion.Euler(0, 0, 180);
        markerInstance.SetActive(true);
    }

    /// <summary>
    /// ターゲットマーカーを非表示にする
    /// </summary>
    public void HideTargetMarker(GameObject markerInstance)
    {
        if (markerInstance != null)
        {
            markerInstance.SetActive(false);
        }
    }
}
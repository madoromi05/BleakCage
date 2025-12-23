using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

/// <summary>
/// バトル開始時のエンティティ（プレイヤー、敵）の生成、配置、参照管理を担う
/// </summary>
public class BattleEntitiesManager : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("デバッグ設定 (Editor Only)")]
    [Tooltip("テストしたいステージID")]
    [SerializeField] private int debugStageID;
#endif

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
    [Header("ステージごとのプレイヤー編成")]
    [SerializeField] private List<StagePlayerSetup> stagePlayerPresets;

    public List<EnemyModel> Enemies { get; private set; } = new List<EnemyModel>();
    public List<PlayerStatusUIController> PlayerStatusUIs { get; private set; } = new List<PlayerStatusUIController>();
    public List<EnemyStatusUIController> EnemyStatusUIs { get; private set; } = new List<EnemyStatusUIController>();
    public Dictionary<PlayerModel, PlayerController> PlayerControllers { get; private set; } = new Dictionary<PlayerModel, PlayerController>();
    public Dictionary<EnemyModel, EnemyController> EnemyControllers { get; private set; } = new Dictionary<EnemyModel, EnemyController>();
    public StageEnemyData CurrentStage { get; private set; }
    public DeckSetupRepository LoadedDeckData { get; private set; }
    public bool IsTutorialMode { get; private set; } = false;
    public List<PlayerRuntime> Players => LoadedDeckData?.Party;

    private BattleManager battleManager;

    public void Setup(BattleManager manager)
    {
        this.battleManager = manager;

        // 今回使用するステージIDをここで一元管理して決定する
        int targetStageID = ResolveStageID();
        Debug.Log($"BattleEntitiesManager: Stage ID {targetStageID} でバトルを開始します。");

        // 決定したIDを渡してロードを実行
        LoadPlayerGameData(targetStageID);
        SetupStageEnemies(targetStageID);
        enemyView();
        playerView();
        IsTutorialMode = (targetStageID == 0);
    }

    /// <summary>
    /// 最終的にどのステージIDを使うか決定するメソッド
    /// </summary>
    private int ResolveStageID()
    {
#if UNITY_EDITOR
        Debug.Log($"<color=yellow>【Debug】Inspector指定の StageID: {debugStageID} を使用します。</color>");
        return debugStageID;
#else
    if (StageManager.SelectedStageID != -1)
    {
        return StageManager.SelectedStageID;
    }
    return -1;
#endif
    }


    /// <summary>
    /// ステージIDに対応したプレイヤーデータをロード・初期化
    /// </summary>
    private void LoadPlayerGameData(int stageID)
    {
        if (stagePlayerPresets == null || stagePlayerPresets.Count == 0)
        {
            Debug.LogError("PlayerPresets が設定されていません！");
            return;
        }

        if (stageID < 0 || stageID >= stagePlayerPresets.Count)
        {
            Debug.LogError($"StageID {stageID} に対応する PlayerPreset がありません。Stage 1 (Index 1) を使用します。");
            stageID = (stagePlayerPresets.Count > 1) ? 1 : 0;
        }

        StagePlayerSetup targetPreset = stagePlayerPresets[stageID];
        var dataLoader = new PlayerDataLoader();
        LoadedDeckData = dataLoader.LoadFromPreset(targetPreset);
    }

    /// <summary>
    /// 指定されたIDで敵ステージデータをセット
    /// </summary>
    private void SetupStageEnemies(int stageID)
    {
        CurrentStage = allStageEnemyData.FirstOrDefault(stage => stage.stageEnemyID == stageID);

        if (CurrentStage == null)
        {
            Debug.LogError($"ID {stageID} に一致する StageEnemyData が見つかりません！");
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

            var statusUIObject = Instantiate(enemyStatusUIPrefab, enemyStatusBarTransform, false);
            EnemyStatusUIController uiController = statusUIObject.GetComponent<EnemyStatusUIController>();
            uiController.SetEnemyStatus(enemy);
            enemyController.SetStatusUI(uiController);
            EnemyStatusUIs.Add(uiController);
        }
    }

    private void playerView()
    {
        if (Players == null || Players.Count == 0) return;

        if (playerPositions == null || playerPositions.Count == 0)
        {
            Debug.LogError("プレイヤーの出現位置が設定されていません！");
            return;
        }

        for (int i = 0; i < Players.Count; i++)
        {
            if (i >= playerPositions.Count || playerPositions[i] == null)
            {
                Debug.LogWarning($"プレイヤー {i + 1} のための出現位置(playerPositions[{i}])が設定されていません。");
                break;
            }

            PlayerRuntime targetPlayer = Players[i];
            Transform spawnPoint = playerPositions[i];
            SpawnPlayerCharacter(targetPlayer, spawnPoint);

            if (targetPlayer.HPHandler != null)
            {
                targetPlayer.HPHandler.OnDead += battleManager.OnPlayerDead;
            }
        }
    }

    private void SpawnPlayerCharacter(PlayerRuntime runtime, Transform spawnPoint)
    {
        var playerObject = Instantiate(playerBasePrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
        PlayerController playerController = playerObject.GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError($"'Player Base Prefab' ({playerBasePrefab.name}) に PlayerController コンポーネントがアタッチされていません！", playerBasePrefab);
            return;
        }

        playerController.Init(runtime.PlayerModel);
        if (runtime.EquippedWeapon != null)
        {
            playerController.SetInitialWeapon(runtime.EquippedWeapon);
            Debug.Log($"[View] 初期武器を表示しました: {runtime.EquippedWeapon.Model.Name}");
        }
        else
        {
            Debug.LogWarning($"[View] {runtime.PlayerModel.PlayerName} は装備武器(EquippedWeapon)を持っていません。");
        }
        runtime.PlayerController = playerController;
        PlayerControllers.Add(runtime.PlayerModel, playerController);

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
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// バトル開始時のエンティティ（プレイヤー、敵）の生成、配置、参照管理を担うクラス
/// </summary>
public class BattleEntitiesManager : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("デバッグ設定 (Editor Only)")]
    [SerializeField] private bool _useDebugSettings = false; // インスペクターからの強制デバッグ有効化
    [SerializeField] private int _debugStageID;
#endif

    [Header("プレハブ と キャラ出現地点")]
    [SerializeField] private GameObject _enemyBasePrefab;
    [SerializeField] private GameObject _playerBasePrefab;
    [SerializeField] private List<Transform> _playerPositions;
    [SerializeField] private List<Transform> _enemyPositions;
    [SerializeField] private GameObject _playerStatusUIPrefab;
    [SerializeField] private GameObject _enemyStatusUIPrefab;

    [Header("UI関連")]
    [SerializeField] private Transform _playerStatusBarTransform;
    [SerializeField] private Transform _enemyStatusBarTransform;

    [Header("マスターデータ")]
    [SerializeField] private List<StageEnemyData> _allStageEnemyData;
    [Header("ステージごとのプレイヤー編成プリセット")]
    [SerializeField] private List<StagePlayerSetup> _stagePlayerPresets;

    public List<EnemyModel> Enemies { get; private set; } = new List<EnemyModel>();
    public List<PlayerStatusUIController> PlayerStatusUIs { get; private set; } = new List<PlayerStatusUIController>();
    public List<EnemyStatusUIController> EnemyStatusUIs { get; private set; } = new List<EnemyStatusUIController>();
    public Dictionary<PlayerModel, PlayerController> PlayerControllers { get; private set; } = new Dictionary<PlayerModel, PlayerController>();
    public Dictionary<EnemyModel, EnemyController> EnemyControllers { get; private set; } = new Dictionary<EnemyModel, EnemyController>();
    public StageEnemyData CurrentStage { get; private set; }
    public DeckSetupRepository LoadedDeckData { get; private set; }
    public bool IsTutorialMode { get; private set; } = false;
    public List<PlayerRuntime> Players => LoadedDeckData?.Party;

    private BattleManager _battleManager;

    /// <summary>
    /// バトル開始前のセットアップを一括実行します
    /// </summary>
    /// <param name="manager">バトル全体を管理するBattleManager</param>
    public void Setup(BattleManager manager)
    {
        _battleManager = manager;
        int targetStageID = DetermineStageID();
        PlayStageBGM(targetStageID);
        LoadPlayerGameData(targetStageID);
        SetupStageEnemyData(targetStageID);
        CreateEnemies();
        CreatePlayers();

        IsTutorialMode = (targetStageID == 0);

        DebugCostom.Log($"BattleEntitiesManager: Stage ID {targetStageID} でバトルを開始します。");
    }

    /// <summary>
    /// 実行環境や設定に基づき、最終的に使用するステージIDを決定します
    /// </summary>
    private int DetermineStageID()
    {
#if UNITY_EDITOR
        // デバッグ設定が有効な場合はそちらを優先
        if (_useDebugSettings)
        {
            DebugCostom.Log($"<color=yellow>【Debug (強制)】Inspector指定の StageID: {_debugStageID} を使用します。</color>");
            return _debugStageID;
        }

        // ホーム画面などから正しく遷移の場合
        if (StageManager.SelectedStageID != -1)
        {
            return StageManager.SelectedStageID;
        }

        DebugCostom.Log($"<color=yellow>【Debug】デフォルトの StageID: {_debugStageID} を使用します。</color>");
        return _debugStageID;
#else
        // ビルド環境では静的マネージャーの値を優先
        if (StageManager.SelectedStageID != -1)
        {
            return StageManager.SelectedStageID;
        }
        return -1;
#endif
    }

    /// <summary>
    /// ステージIDに対応したプレイヤー編成データをロードします
    /// </summary>
    private void LoadPlayerGameData(int stageID)
    {
        if (_stagePlayerPresets == null || _stagePlayerPresets.Count == 0)
        {
            DebugCostom.LogError("PlayerPresets が設定されていません！");
            return;
        }

        if (stageID < 0 || stageID >= _stagePlayerPresets.Count)
        {
            DebugCostom.LogError($"StageID {stageID} に対応する PlayerPreset がありません。デフォルトを使用します。");
            stageID = (_stagePlayerPresets.Count > 1) ? 1 : 0;
        }

        StagePlayerSetup targetPreset = _stagePlayerPresets[stageID];
        PlayerDataLoader dataLoader = new PlayerDataLoader();
        LoadedDeckData = dataLoader.LoadFromPreset(targetPreset);
    }

    /// <summary>
    /// ステージIDに紐づく敵編成データをリストから取得します
    /// </summary>
    private void SetupStageEnemyData(int stageID)
    {
        CurrentStage = _allStageEnemyData.FirstOrDefault(stage => stage.stageEnemyID == stageID);

        if (CurrentStage == null)
        {
            DebugCostom.LogError($"ID {stageID} に一致する StageEnemyData が見つかりません！");
        }
    }

    /// <summary>
    /// 敵エンティティの生成、配置、UIの紐付けを行います
    /// </summary>
    private void CreateEnemies()
    {
        if (CurrentStage == null) return;

        EnemyModelFactory enemyFactory = new EnemyModelFactory();

        for (int i = 0; i < CurrentStage.enemyIDs.Count; i++)
        {
            if (i >= _enemyPositions.Count) break;

            int enemyId = CurrentStage.enemyIDs[i];
            EnemyModel enemy = enemyFactory.CreateFromId(enemyId);
            Enemies.Add(enemy);

            // 敵オブジェクトの生成
            Transform spawnPoint = _enemyPositions[i];
            GameObject enemyObject = Instantiate(_enemyBasePrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
            EnemyController enemyController = enemyObject.GetComponent<EnemyController>();

            // プレイヤー（通常は最初の位置）をターゲットとして初期化
            if (_playerPositions.Count > 0 && _playerPositions[0] != null)
            {
                enemyController.Init(enemy, _playerPositions[0]);
            }

            EnemyControllers.Add(enemy, enemyController);

            // ステータスUIの生成と紐付け
            GameObject statusUIObject = Instantiate(_enemyStatusUIPrefab, _enemyStatusBarTransform, false);
            EnemyStatusUIController uiController = statusUIObject.GetComponent<EnemyStatusUIController>();

            uiController.SetEnemyStatus(enemy);
            enemyController.SetStatusUI(uiController);
            EnemyStatusUIs.Add(uiController);
        }
    }

    /// <summary>
    /// 味方プレイヤーエンティティの生成と初期化を行います
    /// </summary>
    private void CreatePlayers()
    {
        if (Players == null || Players.Count == 0) return;

        if (_playerPositions == null || _playerPositions.Count == 0)
        {
            DebugCostom.LogError("プレイヤーの出現位置が設定されていません！");
            return;
        }

        for (int i = 0; i < Players.Count; i++)
        {
            if (i >= _playerPositions.Count || _playerPositions[i] == null)
            {
                DebugCostom.LogWarning($"プレイヤー {i + 1} の出現位置が足りません。");
                break;
            }

            PlayerRuntime targetPlayer = Players[i];
            Transform spawnPoint = _playerPositions[i];

            SpawnPlayerCharacter(targetPlayer, spawnPoint);

            if (targetPlayer.playerHpHandler != null)
            {
                targetPlayer.playerHpHandler.OnDead += _battleManager.OnPlayerDead;
            }
        }
    }

    /// <summary>
    /// 個別のプレイヤーキャラクターを生成し、コントローラーとUIを設定します
    /// </summary>
    private void SpawnPlayerCharacter(PlayerRuntime runtime, Transform spawnPoint)
    {
        GameObject playerObject = Instantiate(_playerBasePrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
        PlayerController playerController = playerObject.GetComponent<PlayerController>();

        if (playerController == null)
        {
            DebugCostom.LogError($"Prefab ({_playerBasePrefab.name}) に PlayerController がありません。", _playerBasePrefab);
            return;
        }

        playerController.Init(runtime.PlayerModel);
        if (runtime.EquippedWeapon != null)
        {
            playerController.SetInitialWeapon(runtime.EquippedWeapon);
        }

        runtime.PlayerController = playerController;
        PlayerControllers.Add(runtime.PlayerModel, playerController);

        GameObject statusUIObject = Instantiate(_playerStatusUIPrefab, _playerStatusBarTransform, false);
        PlayerStatusUIController playerUiController = statusUIObject.GetComponent<PlayerStatusUIController>();

        if (playerUiController == null) return;

        playerUiController.SetPlayerStatus(runtime);
        playerController.SetStatusUI(playerUiController);
        PlayerStatusUIs.Add(playerUiController);
    }

    /// <summary>
    /// 指定したプレイヤーの頭上にターゲットマーカーを表示します
    /// </summary>
    public void ShowTargetMarkerOnPlayer(GameObject markerInstance, int playerIndex)
    {
        if (markerInstance == null || playerIndex < 0 || playerIndex >= _playerPositions.Count) return;

        Transform playerBase = _playerPositions[playerIndex];
        // 座標計算の定数はマジックナンバーを避けるのが望ましいですが、ここでは一旦維持
        markerInstance.transform.position = playerBase.position + new Vector3(0, 5, 0);
        markerInstance.SetActive(true);
    }

    public void HideTargetMarker(GameObject markerInstance)
    {
        if (markerInstance != null)
        {
            markerInstance.SetActive(false);
        }
    }

    private void PlayStageBGM(int stageID)
    {
        BGMType bgmType = (stageID == 3) ? BGMType.Battle_Boss : BGMType.Battle_Normal;
        SoundManager.Instance.PlayBGM(bgmType);
    }
}
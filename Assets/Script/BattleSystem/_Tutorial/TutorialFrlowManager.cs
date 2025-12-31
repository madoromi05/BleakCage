using UnityEngine;
using System.Collections.Generic;

#if TUTORIAL_ENABLED

/// <summary>
/// チュートリアルのフェーズ進行（Select -> Card -> Enemy）を管理するクラス。
/// 完了後は BattlePhaseManager に処理を移譲します。
/// </summary>
public class TutorialFlowManager : MonoBehaviour
{
    [Header("チュートリアル関連")]
    [SerializeField] private GameObject _tutorialObjectsParent;
    [SerializeField] private GameObject _tutorialUIPanel;
    [SerializeField] private TutorialManager _tutorialManager;
    [SerializeField] private SelectTurnTutorialManager _selectTurnTutorialManager;
    [SerializeField] private EnemyTurnTutorialManager _enemyTurnTutorialManager;
    [SerializeField] private TutorialInputReader _tortrialInputReader;

    // --- 必要なコンポーネント参照 ---
    private BattleManager _battleManager;
    private BattlePhaseManager _normalPhaseManager;
    private BattleEntitiesManager _entitiesManager;
    private EnemyTurn _enemyTurn;
    private List<PlayerRuntime> _players;
    private List<EnemyModel> _enemies;
    private List<PlayerStatusUIController> _playerStatusUIs;
    private List<EnemyStatusUIController> _enemyStatusUIs;
    private List<EnemyRuntime> _enemyRuntimes;

    private SelectTurn _selectTurn;
    private PlayerTurn _playerTurn;
    private BattleCardDeck _battleCardDeck;
    private IPhase _currentPhase;

    /// <summary>
    /// 各マネージャーの初期化と、チュートリアル専用設定の適用を行います。
    /// </summary>
    public void Init(
        BattleManager battleManager, BattlePhaseManager normalPhaseManager,
        BattleEntitiesManager entitiesManager,
        List<PlayerRuntime> players, List<EnemyModel> enemies,
        List<PlayerStatusUIController> playerStatusUIs, List<EnemyStatusUIController> enemyStatusUIs,
        SelectTurn selectTurn, PlayerTurn playerTurn,
        BattleCardDeck battleCardDeck,
        List<EnemyRuntime> enemyRuntimes)
    {
        _battleManager = battleManager;
        _normalPhaseManager = normalPhaseManager;
        _entitiesManager = entitiesManager;
        _players = players;
        _enemies = enemies;
        _playerStatusUIs = playerStatusUIs;
        _enemyStatusUIs = enemyStatusUIs;
        _selectTurn = selectTurn;
        _playerTurn = playerTurn;
        _battleCardDeck = battleCardDeck;
        _enemyRuntimes = enemyRuntimes;

        if (_tutorialObjectsParent != null)
        {
            _tutorialObjectsParent.SetActive(true);
        }

        _selectTurnTutorialManager.Initialize(_tortrialInputReader, players, enemies, playerStatusUIs, enemyStatusUIs);
        _currentPhase = _selectTurnTutorialManager;
        _currentPhase.OnPhaseFinished += OnSelectionPhaseFinished;

        _tutorialManager.Initialize(battleManager, _tortrialInputReader, enemyStatusUIs, _entitiesManager.EnemyControllers, selectTurn);
        _enemyTurnTutorialManager.Initialize(_tortrialInputReader);

        _playerTurn.SetTutorialMode(true);
        _playerTurn.Setup(
            selectTurn.PlayerSelections,
            entitiesManager.Players,
            battleCardDeck,
            entitiesManager.EnemyStatusUIs,
            entitiesManager.PlayerStatusUIs,
            entitiesManager.EnemyControllers,
            enemyRuntimes
        );
    }

    public void StartTutorialFlow()
    {
        _currentPhase.StartPhase();
    }

    /// <summary>
    /// 攻撃対象選択チュートリアルが完了した際のコールバック
    /// </summary>
    private void OnSelectionPhaseFinished()
    {
        _currentPhase.OnPhaseFinished -= OnSelectionPhaseFinished;
        DebugCostom.Log("チュートリアル：カード選択フェーズに移行します。");

        _currentPhase = _tutorialManager;
        _currentPhase.OnPhaseFinished += OnCardTutorialPhaseFinished;
        if (_tutorialUIPanel != null)
        {
            _tutorialUIPanel.SetActive(true);
        }
        _currentPhase.StartPhase();
    }

    /// <summary>
    /// カード選択チュートリアル完了時のコールバック。敵ターンチュートリアルへ移行。
    /// </summary>
    private void OnCardTutorialPhaseFinished()
    {
        _currentPhase.OnPhaseFinished -= OnCardTutorialPhaseFinished;
        DebugCostom.Log("【カードチュートリアル完了】-> 敵ターンチュートリアルへ移行します");

        _currentPhase = _enemyTurnTutorialManager;
        _currentPhase.OnPhaseFinished += OnEnemyTurnTutorialFinished;
        _currentPhase.StartPhase();
    }

    /// <summary>
    /// 敵ターンチュートリアル完了時のコールバック。全てのチュートリアルを終え通常戦闘へ移行。
    /// </summary>
    private void OnEnemyTurnTutorialFinished()
    {
        _currentPhase.OnPhaseFinished -= OnEnemyTurnTutorialFinished;
        DebugCostom.Log("【敵ターンチュートリアル完了】-> 通常戦闘へ移行します");

        _playerTurn.SetTutorialMode(false);

        if (_tutorialObjectsParent != null) _tutorialObjectsParent.SetActive(false);
        if (_tutorialUIPanel != null) _tutorialUIPanel.SetActive(false);

        // --- エラー修正箇所：公開プロパティ経由でアクセス ---
        _normalPhaseManager.Init(
            _entitiesManager,
            _players, _enemies,
            _playerStatusUIs, _enemyStatusUIs,
            _selectTurn,
            _playerTurn,
            _enemyTurn,
            _battleManager.SelectionChoicePanel,
            _battleManager,
            _battleCardDeck,
            _battleManager.GuardGaugeSystem
        );

        // 通常モードのマネージャーを起動
        _normalPhaseManager.gameObject.SetActive(true);
        gameObject.SetActive(false);

        // 最初の選択フェーズを開始
        _normalPhaseManager.StartSelectionPhase();
    }
}
#endif
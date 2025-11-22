using UnityEngine;
using System.Collections.Generic;

#if TUTORIAL_ENABLED

/// <summary>
/// チュートリアルのフェーズ進行（SelectTutorial -> CardTutorial -> EnemyTutorial）のみを管理する
/// 完了後は BattlePhaseManager に処理を移譲する
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

    private SelectTurn _selectTurn;
    private PlayerTurn _playerTurn;
    private BattleCardDeck _battleCardDeck;
    private IPhase _currentPhase;

    // BattleManagerから呼ばれる初期化
    public void Init(
        BattleManager battleManager, BattlePhaseManager normalPhaseManager,
        BattleEntitiesManager entitiesManager,
        List<PlayerRuntime> players, List<EnemyModel> enemies,
        List<PlayerStatusUIController> playerStatusUIs, List<EnemyStatusUIController> enemyStatusUIs,
        SelectTurn selectTurn, PlayerTurn playerTurn,
        BattleCardDeck battleCardDeck)
    {
        // --- 参照の保持 ---
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

        // --- BattlePhaseManager の Init から持ってきたロジック ---
        if (_tutorialObjectsParent != null)
        {
            _tutorialObjectsParent.SetActive(true);
        }

        _selectTurnTutorialManager.Initialize(_tortrialInputReader, players, enemies, playerStatusUIs, enemyStatusUIs);
        _currentPhase = _selectTurnTutorialManager;
        _currentPhase.OnPhaseFinished += OnSelectionPhaseFinished;
        _tutorialManager.Initialize(battleManager, _tortrialInputReader, enemyStatusUIs, _entitiesManager.EnemyControllers, selectTurn);
        _enemyTurnTutorialManager.Initialize(_tortrialInputReader);
        playerTurn.SetTutorialMode(true);
        playerTurn.Setup(selectTurn.PlayerSelections, _players, battleCardDeck, enemyStatusUIs, _entitiesManager.EnemyControllers);
    }

    // チュートリアルフローを開始する
    public void StartTutorialFlow()
    {
        _currentPhase.StartPhase();
    }

    /// <summary>
    /// 攻撃対象選択チュートリアルが完了
    /// </summary>
    private void OnSelectionPhaseFinished()
    {
        _currentPhase.OnPhaseFinished -= OnSelectionPhaseFinished;
        Debug.Log("チュートリアル：カード選択フェーズに移行します。");

        _currentPhase = _tutorialManager;
        _currentPhase.OnPhaseFinished += OnCardTutorialPhaseFinished;
        if (_tutorialUIPanel != null)
        {
            Debug.Log("チュートリアル：UIPanelを表示します。");
            _tutorialUIPanel.SetActive(true);
        }
        _currentPhase.StartPhase();
    }

    /// <summary>
    /// カード選択チュートリアル完了 → 敵ターンチュートリアルへ
    /// </summary>
    private void OnCardTutorialPhaseFinished()
    {
        _currentPhase.OnPhaseFinished -= OnCardTutorialPhaseFinished;
        Debug.Log("【カードチュートリアル完了】-> 敵ターンチュートリアルへ移行します");
        _currentPhase = _enemyTurnTutorialManager;
        _currentPhase.OnPhaseFinished += OnEnemyTurnTutorialFinished;
        _currentPhase.StartPhase();
    }

    /// <summary>
    /// 敵ターンチュートリアル完了 → 通常の戦闘へ
    /// </summary>
    private void OnEnemyTurnTutorialFinished()
    {
        _currentPhase.OnPhaseFinished -= OnEnemyTurnTutorialFinished;
        Debug.Log("【敵ターンチュートリアル完了】-> 通常戦闘へ移行します");

        _playerTurn.SetTutorialMode(false);

        if(_tutorialObjectsParent != null)
        {
            _tutorialObjectsParent.SetActive(false);
        }
        if (_tutorialUIPanel != null)
        {
            _tutorialUIPanel.SetActive(false);
        }

        // 1. 通常モードのフェーズ管理を初期化
        _normalPhaseManager.Init(
            _entitiesManager,
            _players, _enemies,
            _playerStatusUIs, _enemyStatusUIs,
            _selectTurn, 
            _playerTurn,
            _enemyTurn,
            _battleManager.selectionChoicePanel,
            _battleManager,
            _battleCardDeck,
            _battleManager.guardGaugeSystem
);

        // 2. 通常マネージャーをアクティブにし、自分を非アクティブにする
        _normalPhaseManager.gameObject.SetActive(true);
        gameObject.SetActive(false);

        // 3. 通常の選択フェーズを開始
        _normalPhaseManager.StartSelectionPhase();
    }
}
#endif
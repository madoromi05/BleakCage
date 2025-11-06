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
    [SerializeField] private GameObject tutorialObjectsParent;
    [SerializeField] private GameObject tutorialUIPanel;
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private SelectTurnTutorialManager selectTurnTutorialManager;
    [SerializeField] private EnemyTurnTutorialManager enemyTurnTutorialManager;
    [SerializeField] private TutorialInputReader tortrialInputReader;

    // --- 必要なコンポーネント参照 ---
    private BattleManager battleManager;
    private BattlePhaseManager normalPhaseManager; // 通常フローへの切り替え用
    private BattleEntitiesManager entitiesManager;
    private EnemyTurn enemyTurn;
    private List<PlayerRuntime> players;
    private List<EnemyModel> enemies;
    private List<PlayerStatusUIController> playerStatusUIs;
    private List<EnemyStatusUIController> enemyStatusUIs;

    private SelectTurn selectTurn; // TutorialManager に渡すため
    private PlayerTurn playerTurn;
    private BattleCardDeck battleCardDeck;

    private IPhase currentPhase;

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
        this.battleManager = battleManager;
        this.normalPhaseManager = normalPhaseManager;
        this.entitiesManager = entitiesManager;
        this.players = players;
        this.enemies = enemies;
        this.playerStatusUIs = playerStatusUIs;
        this.enemyStatusUIs = enemyStatusUIs;
        this.selectTurn = selectTurn; // TutorialManagerのInit用
        this.playerTurn = playerTurn; // PlayerTurnのセットアップ用
        this.battleCardDeck = battleCardDeck;

        // --- BattlePhaseManager の Init から持ってきたロジック ---
        if (tutorialObjectsParent != null)
        {
            tutorialObjectsParent.SetActive(true);
        }

        // 1. SelectTurnTutorialManager の初期化
        selectTurnTutorialManager.Initialize(tortrialInputReader, players, enemies, playerStatusUIs, enemyStatusUIs);
        currentPhase = selectTurnTutorialManager;
        currentPhase.OnPhaseFinished += OnSelectionPhaseFinished; // チュートリアル内のSelect完了

        // 2. TutorialManager (カード) の初期化
        tutorialManager.Initialize(battleManager, tortrialInputReader, enemyStatusUIs, this.entitiesManager.EnemyControllers, selectTurn);

        // 3. EnemyTurnTutorialManager の初期化
        enemyTurnTutorialManager.Initialize(tortrialInputReader);

        // 4. PlayerTurn のセットアップ (チュートリアル用にここで実行)
        playerTurn.SetTutorialMode(true);
        playerTurn.Setup(selectTurn.PlayerSelections, battleCardDeck, enemyStatusUIs, this.entitiesManager.EnemyControllers);
    }

    // チュートリアルフローを開始する
    public void StartTutorialFlow()
    {
        // 最初のフェーズ（SelectTurnTutorial）を開始
        currentPhase.StartPhase();
    }

    /// <summary>
    /// 攻撃対象選択チュートリアルが完了
    /// </summary>
    private void OnSelectionPhaseFinished()
    {
        currentPhase.OnPhaseFinished -= OnSelectionPhaseFinished;
        Debug.Log("チュートリアル：カード選択フェーズに移行します。");

        currentPhase = tutorialManager;
        currentPhase.OnPhaseFinished += OnCardTutorialPhaseFinished;
        if (tutorialUIPanel != null)
        {
            tutorialUIPanel.SetActive(true);
        }
        currentPhase.StartPhase();
    }

    /// <summary>
    /// カード選択チュートリアル完了 → 敵ターンチュートリアルへ
    /// </summary>
    private void OnCardTutorialPhaseFinished()
    {
        currentPhase.OnPhaseFinished -= OnCardTutorialPhaseFinished;
        Debug.Log("【カードチュートリアル完了】-> 敵ターンチュートリアルへ移行します");

        // ... (BattlePhaseManager から移動したロジック) ...
        currentPhase = enemyTurnTutorialManager;
        currentPhase.OnPhaseFinished += OnEnemyTurnTutorialFinished;
        // ...
        currentPhase.StartPhase();
    }

    /// <summary>
    /// 敵ターンチュートリアル完了 → 通常の戦闘へ
    /// </summary>
    private void OnEnemyTurnTutorialFinished()
    {
        currentPhase.OnPhaseFinished -= OnEnemyTurnTutorialFinished;
        Debug.Log("【敵ターンチュートリアル完了】-> 通常戦闘へ移行します");

        playerTurn.SetTutorialMode(false);

        if (tutorialObjectsParent != null)
        {
            tutorialObjectsParent.SetActive(false);
        }
        if (tutorialUIPanel != null)
        {
            tutorialUIPanel.SetActive(false);
        }

        // --- ここが重要：通常のBattlePhaseManagerに切り替える ---

        // 1. 通常モードのフェーズ管理を初期化
        // (Initに必要な引数をBattleManagerから渡してもらう)
        normalPhaseManager.Init(
            entitiesManager,
            players, enemies,
            playerStatusUIs, enemyStatusUIs,
            selectTurn, 
            playerTurn,
            this.enemyTurn,
            battleManager.selectionChoicePanel,
            battleManager,
             battleCardDeck,
            battleManager.guardGaugeSystem
);

        // 2. 通常マネージャーをアクティブにし、自分を非アクティブにする
        normalPhaseManager.gameObject.SetActive(true);
        this.gameObject.SetActive(false);

        // 3. 通常の選択フェーズを開始
        normalPhaseManager.StartSelectionPhase();
    }
}
#endif
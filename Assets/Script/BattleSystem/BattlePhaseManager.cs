using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

/// <summary>
/// バトルのフェーズ（Select/Player/Enemy）の切り替えと、エクストラターンなどのフローロジックを管理する
/// </summary>
public class BattlePhaseManager : MonoBehaviour
{
    [Header("UI & Time")]
    [SerializeField] private PhaseAnnouncementUIController phaseUI;

    private SelectTurn selectTurn;
    private PlayerTurn playerTurn;
    private EnemyTurn enemyTurn;
    private GameObject selectionChoicePanel;
    private BattleManager battleManager;
    private BattleEntitiesManager entitiesManager;

    private IPhase currentPhase;
    private List<PlayerRuntime> players;
    private List<EnemyModel> enemies;
    private List<PlayerStatusUIController> playerStatusUIs;
    private List<EnemyStatusUIController> enemyStatusUIs;
    private Coroutine selectionChoiceCoroutine;
    private bool isFirstSelectionPhase = true;
    private bool isTutorialMode = false;
    private int currentTurn = 1;
    private bool isExtraTurnSegmentFinished = false; // エクストラターン用フラグ
    private GuardGaugeSystem guardGaugeSystem; // カウンター回数の取得とリセットのため

#if TUTORIAL_ENABLED
    [Header("チュートリアル関連")]
    [SerializeField] private GameObject tutorialObjectsParent;
    [SerializeField] private GameObject tutorialUIPanel;
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private SelectTurnTutorialManager selectTurnTutorialManager;
    [SerializeField] private EnemyTurnTutorialManager enemyTurnTutorialManager;
    [SerializeField] private TutorialInputReader tortrialInputReader;
#endif

    public void Init(bool isTutorialMode,
                     BattleEntitiesManager entitiesManager,
                     List<PlayerRuntime> players, List<EnemyModel> enemies,
                     List<PlayerStatusUIController> playerStatusUIs, List<EnemyStatusUIController> enemyStatusUIs,
                     SelectTurn selectTurn, PlayerTurn playerTurn, EnemyTurn enemyTurn,
                     GameObject selectionChoicePanel, BattleManager battleManager,
                     BattleCardDeck battleCardDeck, GuardGaugeSystem guardGaugeSystem)
    {
        this.isTutorialMode = isTutorialMode;
        this.entitiesManager = entitiesManager;
        this.players = players;
        this.enemies = enemies;
        this.playerStatusUIs = playerStatusUIs;
        this.enemyStatusUIs = enemyStatusUIs;
        this.selectTurn = selectTurn;
        this.playerTurn = playerTurn;
        this.enemyTurn = enemyTurn;
        this.selectionChoicePanel = selectionChoicePanel;
        this.battleManager = battleManager;
        this.entitiesManager = entitiesManager;
        this.guardGaugeSystem = guardGaugeSystem;

        // フェーズ初期化
        if (isTutorialMode)
        {
#if TUTORIAL_ENABLED
            if (tutorialObjectsParent != null)
            {
                tutorialObjectsParent.SetActive(true);
            }

            // 1. SelectTurnTutorialManager の初期化 (変更なし)
            selectTurnTutorialManager.Initialize(tortrialInputReader, players, enemies, playerStatusUIs, enemyStatusUIs);
            currentPhase = selectTurnTutorialManager;
            currentPhase.OnPhaseFinished += OnSelectionPhaseFinished;

            // 2. TutorialManager (カード) の初期化 (引数を修正
            tutorialManager.Initialize(tortrialInputReader,
                                       enemyStatusUIs,
                                       this.entitiesManager.EnemyControllers,
                                       selectTurn);

            // 3. EnemyTurnTutorialManager の初期化 (引数を修正)
            enemyTurnTutorialManager.Initialize(tortrialInputReader);

            // 4. PlayerTurn のセットアップ (チュートリアル用にここで実行)
            playerTurn.SetTutorialMode(true);
            playerTurn.Setup(selectTurn.PlayerSelections, battleCardDeck, enemyStatusUIs, this.entitiesManager.EnemyControllers);
#endif
        }
        else
        {
            // チュートリアル用オブジェクトを非表示にする
            if (tutorialObjectsParent != null)
            {
                tutorialObjectsParent.SetActive(false);
            }
            InitializeNonTutorialPhases();
        }

        playerTurn.OnTurnFinished += OnPlayerTurnFinished;
        enemyTurn.TurnFinished += OnEnemyTurnFinished;
    }

    /// <summary>
    /// 通常モード（非チュートリアル）のフェーズ初期化
    /// </summary>
    private void InitializeNonTutorialPhases()
    {
        selectTurn.Initialize(players, enemies, playerStatusUIs, enemyStatusUIs);
        currentPhase = selectTurn;
        // currentPhase.OnPhaseFinished の購読は ProcessSelectionPhase 内で行う
    }

    /// <summary>
    /// ターン数とフェーズ名をUIに表示する
    /// </summary>
    public IEnumerator ShowPhaseUI(string phaseName)
    {
        if (phaseUI != null)
        {
            yield return StartCoroutine(phaseUI.ShowPhaseAnnouncement(currentTurn, phaseName));
        }
        else
        {
            Debug.LogWarning("PhaseAnnouncementUIController が設定されていません。");
            yield return null;
        }
    }

    // --- ターン進行 ---
    public void StartSelectionPhase()
    {
        Debug.Log("【攻撃対象選択ターン開始】");

        if (isFirstSelectionPhase)
        {
            isFirstSelectionPhase = false;
            StartCoroutine(ProcessSelectionPhase(keepSelections: false));
        }
        else
        {
            if (selectionChoicePanel != null)
            {
                selectionChoicePanel.SetActive(true);
                selectionChoiceCoroutine = StartCoroutine(WaitForSelectionChoice());
            }
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

        if (isTutorialMode)
        {
#if TUTORIAL_ENABLED
            Debug.Log("チュートリアル：カード選択フェーズに移行します。");

            currentPhase = tutorialManager;
            currentPhase.OnPhaseFinished += OnCardTutorialPhaseFinished;
            if (tutorialUIPanel != null)
            {
                tutorialUIPanel.SetActive(true);
            }
            currentPhase.StartPhase();
#endif
        }
        else
        {
            // 通常モードの場合、プレイヤーのカード選択ターンを開始
            StartCoroutine(StartPlayerTurnCoroutine());
        }
    }

    public IEnumerator StartPlayerTurnCoroutine(string phaseName = "Player Phase")
    {
        // PlayerTurnのStartPlayerTurnWithTimerはBattleManagerで実行
        yield return battleManager.StartPlayerTurnWithTimer(phaseName);
    }

    private void OnPlayerTurnFinished()
    {
        Debug.Log("【カード選択ターン終了】");
        if (!isTutorialMode)
        {
            StartCoroutine(EnemyTurnCoroutine());
        }
    }

    private IEnumerator EnemyTurnCoroutine()
    {
        yield return StartCoroutine(ShowPhaseUI("Enemy Phase"));
        Debug.Log("【敵ターン開始】");
        enemyTurn.StartEnemyTurn();
        yield return null;
    }

    private void OnEnemyTurnFinished()
    {
        Debug.Log("【敵ターン終了】");
        currentTurn++;

        int counterCount = guardGaugeSystem.PopCounterCount(); // カウンター回数を取得・リセット

        if (counterCount > 0)
        {
            Debug.Log($"カウンターが {counterCount} 回あります。エクストラターンに移行します。");
            StartCoroutine(HandleExtraTurnsAndContinue(counterCount));
        }
        else
        {
            Debug.Log("カウンターはありません。通常の選択フェーズに移行します。");
            StartSelectionPhase();
        }
    }

    // --- エクストラターン関連 ---

    private void OnExtraTurnFinished()
    {
        Debug.Log("【エクストラターン カード選択/攻撃 完了】");
        isExtraTurnSegmentFinished = true;
    }

    private IEnumerator HandleExtraTurnsAndContinue(int initialCounterCount)
    {
        playerTurn.OnTurnFinished -= OnPlayerTurnFinished;
        playerTurn.OnTurnFinished += OnExtraTurnFinished;

        int remainingCounters = initialCounterCount;
        while (remainingCounters > 0)
        {
            remainingCounters--;
            Debug.Log($"エクストラターン開始！ (残り: {remainingCounters})");

            isExtraTurnSegmentFinished = false;
            // エクストラターン開始
            yield return battleManager.StartPlayerTurnWithTimer("Extra Turn");

            // エクストラターンが完了するまで待機
            yield return new WaitUntil(() => isExtraTurnSegmentFinished == true);
        }

        playerTurn.OnTurnFinished -= OnExtraTurnFinished;
        playerTurn.OnTurnFinished += OnPlayerTurnFinished;

        StartSelectionPhase();
    }

    // --- 選択フェーズのロジック (BattleManagerから移動) ---

    private IEnumerator ProcessSelectionPhase(bool keepSelections)
    {
        // 継続処理が有効でない、または強制選択の場合
        if (!keepSelections || (isTutorialMode && isFirstSelectionPhase))
        {
            if (!keepSelections)
            {
                yield return StartCoroutine(ShowPhaseUI("Select Phase"));
            }
        }

        currentPhase.OnPhaseFinished += OnSelectionPhaseFinished;

        if (currentPhase is SelectTurn concreteSelectTurn)
        {
            concreteSelectTurn.StartPhase(keepSelections);
        }
        else
        {
            currentPhase.StartPhase();
        }
    }

    private IEnumerator WaitForSelectionChoice()
    {
        Debug.Log("優先順位の選択（継続/変更）を待機中...");
        yield return new WaitUntil(() => selectionChoicePanel.activeSelf == false);
        selectionChoiceCoroutine = null;
    }

    public void OnKeepSelections()
    {
        selectionChoicePanel.SetActive(false);
        if (selectionChoiceCoroutine != null)
        {
            StopCoroutine(selectionChoiceCoroutine);
            selectionChoiceCoroutine = null;
        }
        StartCoroutine(ProcessSelectionPhase(keepSelections: true));
    }

    public void OnChangeSelections()
    {
        selectionChoicePanel.SetActive(false);
        if (selectionChoiceCoroutine != null)
        {
            StopCoroutine(selectionChoiceCoroutine);
            selectionChoiceCoroutine = null;
        }
        StartCoroutine(ProcessSelectionPhase(keepSelections: false));
    }

#if TUTORIAL_ENABLED
    /// <summary>
    /// カード選択チュートリアル完了 → 敵ターンチュートリアルへ
    /// </summary>
    private void OnCardTutorialPhaseFinished()
    {
        if (currentPhase != null)
        {
            currentPhase.OnPhaseFinished -= OnCardTutorialPhaseFinished;
        }
        Debug.Log("【カードチュートリアル完了】-> 敵ターンチュートリアルへ移行します");

        if (tutorialUIPanel != null)
        {
            tutorialUIPanel.SetActive(false);
        }

        currentPhase = enemyTurnTutorialManager;
        currentPhase.OnPhaseFinished += OnEnemyTurnTutorialFinished;

        if (tutorialUIPanel != null)
        {
            tutorialUIPanel.SetActive(true);
        }
        currentPhase.StartPhase();
    }

    /// <summary>
    /// 敵ターンチュートリアル完了 → 通常の戦闘へ
    /// </summary>
    private void OnEnemyTurnTutorialFinished()
    {
        if (currentPhase != null)
        {
            currentPhase.OnPhaseFinished -= OnEnemyTurnTutorialFinished;
        }
        Debug.Log("【敵ターンチュートリアル完了】-> 通常戦闘へ移行します");

        isTutorialMode = false;
        playerTurn.SetTutorialMode(false);

        if (tutorialObjectsParent != null)
        {
            tutorialObjectsParent.SetActive(false);
        }
        if (tutorialUIPanel != null)
        {
            tutorialUIPanel.SetActive(false);
        }

        // 通常モードのフェーズ管理に切り替え
        selectTurn.Initialize(players, enemies, playerStatusUIs, enemyStatusUIs);
        currentPhase = selectTurn;

        // 通常の選択フェーズを開始
        StartSelectionPhase();
    }
#endif
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    private int currentTurn = 1;
    private bool isExtraTurnSegmentFinished = false; // エクストラターン用フラグ
    private GuardGaugeSystem guardGaugeSystem; // カウンター回数の取得とリセットのため

    public void Init(
                     BattleEntitiesManager entitiesManager,
                     List<PlayerRuntime> players, List<EnemyModel> enemies,
                     List<PlayerStatusUIController> playerStatusUIs, List<EnemyStatusUIController> enemyStatusUIs,
                     SelectTurn selectTurn, PlayerTurn playerTurn, EnemyTurn enemyTurn,
                     GameObject selectionChoicePanel, BattleManager battleManager,
                     BattleCardDeck battleCardDeck, GuardGaugeSystem guardGaugeSystem)
    {
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

        InitializeNonTutorialPhases();

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

        StartCoroutine(StartPlayerTurnCoroutine());
    }

    public IEnumerator StartPlayerTurnCoroutine(string phaseName = "Player Phase")
    {
        // PlayerTurnのStartPlayerTurnWithTimerはBattleManagerで実行
        yield return battleManager.StartPlayerTurnWithTimer(phaseName);
    }

    private void OnPlayerTurnFinished()
    {
        Debug.Log("【カード選択ターン終了】");
        StartCoroutine(EnemyTurnCoroutine());
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
        if (!keepSelections)
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
}
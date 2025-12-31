using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

/// <summary>
/// バトルのフェーズ（Select/Player/Enemy）の切り替えと、エクストラターンなどのフローロジックを管理する
/// </summary>
public class BattlePhaseManager : MonoBehaviour
{
    [Header("UI & Time")]
    [SerializeField] private PhaseAnnouncementUIController _phaseUI;
    [SerializeField] private BattleInputReader _inputReader;

    private SelectTurn _selectTurn;
    private PlayerTurn _playerTurn;
    private EnemyTurn _enemyTurn;
    private GameObject _selectionChoicePanel;
    private BattleManager _battleManager;
    private BattleEntitiesManager _entitiesManager;

    private IPhase _currentPhase;
    private List<PlayerRuntime> _players;
    private List<EnemyModel> _enemies;
    private List<PlayerStatusUIController> _playerStatusUIs;
    private List<EnemyStatusUIController> _enemyStatusUIs;
    private Coroutine _selectionChoiceCoroutine;
    private bool _isFirstSelectionPhase = true;
    private int _currentTurn = 1;
    private bool _isExtraTurnSegmentFinished = false;
    private GuardGaugeSystem _guardGaugeSystem;

    public void Init(
                      BattleEntitiesManager entitiesManager,
                      List<PlayerRuntime> players, List<EnemyModel> enemies,
                      List<PlayerStatusUIController> playerStatusUIs, List<EnemyStatusUIController> enemyStatusUIs,
                      SelectTurn selectTurn, PlayerTurn playerTurn, EnemyTurn enemyTurn,
                      GameObject selectionChoicePanel, BattleManager battleManager,
                      BattleCardDeck battleCardDeck, GuardGaugeSystem guardGaugeSystem)
    {
        _entitiesManager = entitiesManager;
        _players = players;
        _enemies = enemies;
        _playerStatusUIs = playerStatusUIs;
        _enemyStatusUIs = enemyStatusUIs;
        _selectTurn = selectTurn;
        _playerTurn = playerTurn;
        _enemyTurn = enemyTurn;
        _selectionChoicePanel = selectionChoicePanel;
        _battleManager = battleManager;
        _guardGaugeSystem = guardGaugeSystem;

        InitializeNonTutorialPhases();

        _playerTurn.OnTurnFinished += OnPlayerTurnFinished;
        _enemyTurn.TurnFinished += OnEnemyTurnFinished;
    }

    /// <summary>
    /// 通常モード（非チュートリアル）のフェーズ初期化
    /// </summary>
    private void InitializeNonTutorialPhases()
    {
        _selectTurn.Initialize(_players, _enemies, _playerStatusUIs, _enemyStatusUIs);
        _currentPhase = _selectTurn;
    }

    /// <summary>
    /// ターン数とフェーズ名をUIに表示する
    /// </summary>
    public IEnumerator ShowPhaseUI(string phaseName)
    {
        if (_phaseUI != null)
        {
            yield return StartCoroutine(_phaseUI.ShowPhaseAnnouncement(_currentTurn, phaseName));
        }
        else
        {
            DebugCostom.LogWarning("PhaseAnnouncementUIController が設定されていません。");
            yield return null;
        }
    }

    // --- ターン進行 ---
    public void StartSelectionPhase()
    {
        if (_battleManager.IsBattleEnded) return;
        DebugCostom.Log("【攻撃対象選択ターン開始】");
        SoundManager.Instance.PlaySE(SEType.startedSelectCard);

        if (_isFirstSelectionPhase)
        {
            _isFirstSelectionPhase = false;
            StartCoroutine(ProcessSelectionPhase(keepSelections: false));
        }
        else
        {
            if (_selectionChoicePanel != null)
            {
                _selectionChoicePanel.SetActive(true);
                _selectionChoiceCoroutine = StartCoroutine(WaitForSelectionChoice());
            }
        }
    }

    /// <summary>
    /// 攻撃対象の選択が完了した時に呼び出される
    /// </summary>
    private void OnSelectionPhaseFinished()
    {
        if (_battleManager.IsBattleEnded) return;
        if (_currentPhase != null)
        {
            _currentPhase.OnPhaseFinished -= OnSelectionPhaseFinished;
        }
        DebugCostom.Log("【攻撃対象選択ターン終了】");


        StartCoroutine(StartPlayerTurnCoroutine());
    }

    public IEnumerator StartPlayerTurnCoroutine(string phaseName = "Player Phase")
    {
        yield return _battleManager.StartPlayerTurnWithTimer(phaseName);
    }

    private void OnPlayerTurnFinished()
    {
        if (_battleManager.IsBattleEnded) return;
        DebugCostom.Log("【カード選択ターン終了】");
        StartCoroutine(ExecuteEnemyTurnCoroutine());
    }

    private IEnumerator ExecuteEnemyTurnCoroutine()
    {
        yield return StartCoroutine(ShowPhaseUI("Enemy Phase"));
        DebugCostom.Log("【敵ターン開始】");
        SoundManager.Instance.PlaySE(SEType.SwitchingPhases);
        _enemyTurn.StartEnemyTurn();
        yield return null;
    }

    private void OnEnemyTurnFinished()
    {
        if (_battleManager.IsBattleEnded) return;
        DebugCostom.Log("【敵ターン終了】");
        _currentTurn++;

        int counterCount = _guardGaugeSystem.PopCounterCount();

        if (counterCount > 0)
        {
            DebugCostom.Log($"カウンターが {counterCount} 回あります。エクストラターンに移行します。");
            StartCoroutine(HandleExtraTurnsAndContinue(counterCount));
        }
        else
        {
            StartSelectionPhase();
        }
    }

    // --- エクストラターン関連 ---

    private void OnExtraTurnFinished()
    {
        DebugCostom.Log("【エクストラターン カード選択/攻撃 完了】");
        _isExtraTurnSegmentFinished = true;
    }

    private IEnumerator HandleExtraTurnsAndContinue(int initialCounterCount)
    {
        _playerTurn.OnTurnFinished -= OnPlayerTurnFinished;
        _playerTurn.OnTurnFinished += OnExtraTurnFinished;

        int remainingCounters = initialCounterCount;
        while (remainingCounters > 0)
        {
            remainingCounters--;
            DebugCostom.Log($"エクストラターン開始！ (残り: {remainingCounters})");

            _isExtraTurnSegmentFinished = false;
            yield return _battleManager.StartPlayerTurnWithTimer("Extra Turn");
            yield return new WaitUntil(() => _isExtraTurnSegmentFinished == true);
        }

        _playerTurn.OnTurnFinished -= OnExtraTurnFinished;
        _playerTurn.OnTurnFinished += OnPlayerTurnFinished;

        StartSelectionPhase();
    }


    private IEnumerator ProcessSelectionPhase(bool keepSelections)
    {
        if (!keepSelections)
        {
            if (!keepSelections)
            {
                yield return StartCoroutine(ShowPhaseUI("Select Phase"));
            }
        }

        SoundManager.Instance.PlaySE(SEType.SwitchingPhases);
        _currentPhase.OnPhaseFinished += OnSelectionPhaseFinished;

        if (_currentPhase is SelectTurn concreteSelectTurn)
        {
            concreteSelectTurn.StartPhase(keepSelections);
        }
        else
        {
            _currentPhase.StartPhase();
        }
    }

    private IEnumerator WaitForSelectionChoice()
    {
        yield return new WaitUntil(() => _selectionChoicePanel.activeSelf == false);
        _selectionChoiceCoroutine = null;
    }

    public void OnKeepSelections()
    {
        DebugCostom.Log("選択を保持して続行が選択されました。");
        _selectionChoicePanel.SetActive(false);
        if (_selectionChoiceCoroutine != null)
        {
            StopCoroutine(_selectionChoiceCoroutine);
            _selectionChoiceCoroutine = null;
        }

        if (_selectTurn != null)
        {
            _selectTurn.ValidateSelections();
        }

        StartCoroutine(ProcessSelectionPhase(keepSelections: true));
    }

    public void OnChangeSelections()
    {
        DebugCostom.Log("選択を変更して続行が選択されました。");
        _selectionChoicePanel.SetActive(false);
        if (_selectionChoiceCoroutine != null)
        {
            StopCoroutine(_selectionChoiceCoroutine);
            _selectionChoiceCoroutine = null;
        }

        if (_selectTurn != null)
        {
            _selectTurn.ClearSelections();
        }

        StartCoroutine(ProcessSelectionPhase(keepSelections: false));
    }
}
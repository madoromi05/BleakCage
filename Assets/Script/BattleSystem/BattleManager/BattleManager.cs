using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Battleのターン、時間とデータ管理 (PhaseManagerへの依存を含む)
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("コアコンポーネント参照")]
    [SerializeField] public BattleCardDeck BattleCardDeck;
    [SerializeField] public GuardGaugeSystem GuardGaugeSystem;

    [SerializeField] private GameObject _gameOverUIPanel;
    [SerializeField] private BattlePhaseManager _normalPhaseManager;
    [SerializeField] private TutorialFlowManager _tutorialFlowManager;
    [SerializeField] private BattleEntitiesManager _entitiesManager;
    [SerializeField] private DefenseFeedbackUI _defenseFeedbackUI;
    [SerializeField] private PlayerTurn _playerTurn;
    [SerializeField] private EnemyTurn _enemyTurn;
    [SerializeField] private SelectTurn _selectTurn;

    [Header("UI関連")]
    [SerializeField] public GameObject SelectionChoicePanel;

    [SerializeField] private Text _timeText;
    [SerializeField] private Button _keepSelectionsButton;
    [SerializeField] private Button _changeSelectionsButton;
    [SerializeField] private GameObject _targetMarkerPrefab;

    public void ShowDefenseFeedback(string message, Color color) => _defenseFeedbackUI.ShowDefenseFeedback(message, color);

    public GameObject MarkerInstance { get; private set; }
    public bool IsBattleEnded { get; private set; } = false;

    private float _playerTurnDuration = 10f;
    private float _turnTime = 10f;
    private Coroutine _feedbackCoroutine;
    private List<EnemyRuntime> _enemyRuntimes = new List<EnemyRuntime>();

    void Start()
    {
        if (_targetMarkerPrefab != null)
        {
            MarkerInstance = Instantiate(_targetMarkerPrefab, Vector3.zero, _targetMarkerPrefab.transform.rotation, this.transform);
            MarkerInstance.SetActive(false);
        }

        SelectionChoicePanel.SetActive(false);

        GuardGaugeSystem.Init();
        _entitiesManager.Setup(this);

        InitializeBattleFlow();
    }

    /// <summary>
    /// 通常モードかチュートリアルモードか確認して、
    /// 適切なフェーズマネージャーへ処理を委譲する
    /// </summary>
    private void InitializeBattleFlow()
    {
        if (_entitiesManager.LoadedDeckData == null || _entitiesManager.LoadedDeckData.Party == null)
        {
            DebugCostom.LogError("entitiesManager にデッキデータ(Party)がロードされていません！");
            return;
        }

        List<CardRuntime> allCards = new List<CardRuntime>();
        foreach (var player in _entitiesManager.LoadedDeckData.Party)
        {
            allCards.AddRange(player.GetAllCards());
        }
        BattleCardDeck.InitFromCardList(allCards);

        Dictionary<PlayerRuntime, PlayerController> runtimeControllerMap = new Dictionary<PlayerRuntime, PlayerController>();
        foreach (var runtime in _entitiesManager.Players)
        {
            if (_entitiesManager.PlayerControllers.TryGetValue(runtime.PlayerModel, out var controller))
            {
                runtimeControllerMap[runtime] = controller;
            }
        }

        _enemyRuntimes.Clear();
        if (_entitiesManager.Enemies != null)
        {
            foreach (var enemyModel in _entitiesManager.Enemies)
            {
                EnemyRuntime newRuntime = new EnemyRuntime(enemyModel, System.Guid.NewGuid().ToString());
                _enemyRuntimes.Add(newRuntime);

                if (newRuntime.HpHandler != null)
                {
                    newRuntime.HpHandler.OnDead += OnEnemyDead;
                }
            }
        }

        _enemyTurn.EnemySetup(
            _entitiesManager.Players,
            _entitiesManager.Enemies,
            _entitiesManager.EnemyControllers,
            runtimeControllerMap,
            _entitiesManager.PlayerStatusUIs,
            _enemyRuntimes
        );
        // チュートリアルフローの分岐
        bool isTutorial = _entitiesManager.IsTutorialMode;

        if (isTutorial)
        {
            // --- チュートリアルフロー開始 ---
            _tutorialFlowManager.gameObject.SetActive(true);
            _tutorialFlowManager.Init(
                this,
                _normalPhaseManager,
                _entitiesManager,
                _entitiesManager.Players,
                _entitiesManager.Enemies,
                _entitiesManager.PlayerStatusUIs,
                _entitiesManager.EnemyStatusUIs,
                _selectTurn,
                _playerTurn,
                BattleCardDeck,
                _enemyRuntimes
            );

            _tutorialFlowManager.StartTutorialFlow();
        }
        else
        {
            // --- 通常フロー開始 ---
            _normalPhaseManager.gameObject.SetActive(true);
            _tutorialFlowManager.gameObject.SetActive(false);
            _normalPhaseManager.Init(
                _entitiesManager,
                _entitiesManager.Players,
                _entitiesManager.Enemies,
                _entitiesManager.PlayerStatusUIs,
                _entitiesManager.EnemyStatusUIs,
                _selectTurn,
                _playerTurn,
                _enemyTurn,
                SelectionChoicePanel,
                this,
                BattleCardDeck,
                this.GuardGaugeSystem
            );

            _normalPhaseManager.StartSelectionPhase();
        }
    }

    public void OnPlayerDead(PlayerRuntime player)
    {
        if (IsBattleEnded) return;
        if (player == null) return;

        DebugCostom.Log($"味方死亡: {player.PlayerModel.PlayerName}");

        _selectTurn?.RemovePlayerFromSelections(player);
        var pui = _entitiesManager.PlayerStatusUIs
            .FirstOrDefault(u => u != null && u.GetPlayerRuntime() == player);
        if (pui != null) pui.gameObject.SetActive(false);

        // 死亡演出（コントローラーのアニメ再生へ）
        if (_entitiesManager.PlayerControllers.TryGetValue(player.PlayerModel, out var controller))
        {
            var death = controller.GetComponent<PlayerDeathController>();

            if (death != null)
            {
                StartCoroutine(death.DeadSequence());
                DebugCostom.Log($"[OnPlayerDead] Started DeadSequence for player={controller.name}", controller);
            }
            else
            {
                controller.PlayDeadAnimation();
            }
        }

        bool allPlayersDead = _entitiesManager.Players.All(p => p.CurrentHP <= 0);
        if (allPlayersDead)
        {
            StartCoroutine(BattleLoseProcess());
        }
    }

    private IEnumerator BattleWinProcess()
    {
        IsBattleEnded = true;
        Debug.Log("【BATTLE WIN】");
        yield return new WaitForSeconds(1.5f);

        // チュートリアルの場合は終了しない
        if (_entitiesManager.IsTutorialMode)
        {
            SceneManager.LoadScene("HomeScene");
            yield break;
        }
        // 現在のステージをクリア済みのし、セーブデータを更新
        StageManager.OnStageCleared(StageManager.SelectedStageID);
        StageManager.IsPostBattle = true;
        SceneManager.LoadScene("ScenarioScene");
    }

    private IEnumerator BattleLoseProcess()
    {
        IsBattleEnded = true;
        yield return new WaitForSeconds(1.5f);
        if (_gameOverUIPanel != null)
        {
            _gameOverUIPanel.SetActive(true);
        }

        yield return new WaitForSeconds(3.0f);
        SceneManager.LoadScene("HomeScene");
    }

    public IEnumerator StartPlayerTurnWithTimer(string phaseName = "Player Phase")
    {
        if (IsBattleEnded) yield break;

        yield return StartCoroutine(_normalPhaseManager.ShowPhaseUI(phaseName));
        DebugCostom.Log("【カード入力ターン開始】");
        _timeText.enabled = true;

        _playerTurn.Setup(
            _selectTurn.PlayerSelections,
            _entitiesManager.Players,
            BattleCardDeck,
            _entitiesManager.EnemyStatusUIs,
            _entitiesManager.PlayerStatusUIs,
            _entitiesManager.EnemyControllers,
            _enemyRuntimes
        );

        _playerTurn.StartPlayerTurn();

        _turnTime = _playerTurnDuration;
        float soundTime = 1f;

        // playerTurn.isTurnFinished -> IsTurnFinished (前回の修正を反映)
        while (_turnTime >= 0 && !_playerTurn.IsTurnFinished && !IsBattleEnded)
        {
            if (soundTime <= 0f)
            {
                SoundManager.Instance.PlaySE(SEType.CountDown);
                soundTime = 1f;
            }
            _turnTime -= Time.deltaTime;
            soundTime -= Time.deltaTime;
            _timeText.text = _turnTime.ToString("f2") + " <size=70%>SECOND</size>";
            yield return null;
        }
        _turnTime = 0f;
        _timeText.text = _turnTime.ToString("f2") + " <size=70%>SECOND</size>";
        if (!IsBattleEnded)
        {
            _playerTurn.FinishPlayerTurn();
        }
    }
    public void OnEnemyDead(EnemyRuntime enemy)
    {
        if (IsBattleEnded) return;
        if (enemy == null) return;

        DebugCostom.Log($"敵死亡: {enemy.EnemyModel.EnemyName}");

        if (_enemyRuntimes.Contains(enemy))
        {
            _enemyRuntimes.Remove(enemy);
        }

        if (_selectTurn != null)
        {
            _selectTurn.RemoveEnemyFromSelections(enemy.EnemyModel);
        }

        if (_entitiesManager.EnemyControllers.TryGetValue(enemy.EnemyModel, out EnemyController controller))
        {
            StartCoroutine(controller.DeadSequence());

            var ui = _entitiesManager.EnemyStatusUIs
                .FirstOrDefault(u => u != null && u.GetEnemyModel() == enemy.EnemyModel);
            if (ui != null) ui.gameObject.SetActive(false);
        }

        // 勝利判定
        bool allEnemiesDead = _enemyRuntimes.All(e => e.CurrentHP <= 0);
        if (allEnemiesDead)
        {
            StartCoroutine(BattleWinProcess());
        }
    }
}
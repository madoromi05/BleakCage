using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Battleのターン、順番とデータ管理 (PhaseManagerへの委譲が主)
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("コアコンポーネント参照")]
    [SerializeField] public BattleCardDeck battleCardDeck;
    [SerializeField] public GuardGaugeSystem guardGaugeSystem;
    [SerializeField] private GameObject gameOverUIPanel;
    [SerializeField] private BattlePhaseManager normalPhaseManager;
    [SerializeField] private TutorialFlowManager tutorialFlowManager;
    [SerializeField] private BattleEntitiesManager entitiesManager;
    [SerializeField] private DefenseFeedbackUI defenseFeedbackUI;
    [SerializeField] private PlayerTurn playerTurn;
    [SerializeField] private EnemyTurn enemyTurn;
    [SerializeField] private SelectTurn selectTurn;

    [Header("UI関連")]
    [SerializeField] public GameObject selectionChoicePanel;
    [SerializeField] private Text timeText;
    [SerializeField] private Button keepSelectionsButton;
    [SerializeField] private Button changeSelectionsButton;
    [SerializeField] private GameObject targetMarkerPrefab;

    public void ShowDefenseFeedback(string message, Color color) => defenseFeedbackUI.ShowDefenseFeedback(message, color);

    public GameObject MarkerInstance { get; private set; }
    public bool IsBattleEnded { get; private set; } = false;
    private float playerTurnDuration = 10f; // PhaseManager に移動しても良い
    private float turnTime = 10f; // PlayerTurnWithTimer で使用
    private Coroutine feedbackCoroutine;
    private List<EnemyRuntime> enemyRuntimes = new List<EnemyRuntime>();
    void Start()
    {
        if (targetMarkerPrefab != null)
        {
            MarkerInstance = Instantiate(targetMarkerPrefab, Vector3.zero, Quaternion.identity, this.transform);
            MarkerInstance.SetActive(false);
        }

        selectionChoicePanel.SetActive(false);

        guardGaugeSystem.Init();
        entitiesManager.Setup(this);

        InitializeBattleFlow();
    }

    /// <summary>
    /// 通常モードかチュートリアルモードに応じて、
    /// 起動するフローマネージャーを切り替える
    /// </summary>
    private void InitializeBattleFlow()
    {
        if (entitiesManager.LoadedDeckData == null || entitiesManager.LoadedDeckData.Party == null)
        {
            Debug.LogError("entitiesManager がデッキデータ(Party)をロードしていません！");
            return;
        }

        List<CardRuntime> allCards = new List<CardRuntime>();
        foreach (var player in entitiesManager.LoadedDeckData.Party)
        {
            allCards.AddRange(player.GetAllCards());
        }
        battleCardDeck.InitFromCardList(allCards);

        Dictionary<PlayerRuntime, PlayerController> runtimeControllerMap = new Dictionary<PlayerRuntime, PlayerController>();
        foreach (var runtime in entitiesManager.Players)
        {
            if (entitiesManager.PlayerControllers.TryGetValue(runtime.PlayerModel, out var controller))
            {
                runtimeControllerMap[runtime] = controller;
            }
        }

        enemyRuntimes.Clear();
        if (entitiesManager.Enemies != null)
        {
            foreach (var enemyModel in entitiesManager.Enemies)
            {
                // GUIDとRuntimeを生成
                EnemyRuntime newRuntime = new EnemyRuntime(enemyModel, System.Guid.NewGuid().ToString());
                enemyRuntimes.Add(newRuntime);

                if (newRuntime.HPHandler != null)
                {
                    newRuntime.HPHandler.OnDead += OnEnemyDead;
                }
            }
        }

        enemyTurn.EnemySetup(
            entitiesManager.Players,
            entitiesManager.Enemies,
            entitiesManager.EnemyControllers,
            runtimeControllerMap,
            entitiesManager.PlayerStatusUIs,
            enemyRuntimes
        );
        // === フローの分岐 ===
        bool isTutorial = entitiesManager.IsTutorialMode;

        if (isTutorial)
        {
            // --- チュートリアルフローを開始 ---
            tutorialFlowManager.gameObject.SetActive(true);
            tutorialFlowManager.Init(
                this,
                normalPhaseManager,
                entitiesManager,
                entitiesManager.Players,
                entitiesManager.Enemies,
                entitiesManager.PlayerStatusUIs,
                entitiesManager.EnemyStatusUIs,
                selectTurn,
                playerTurn,
                battleCardDeck,
                enemyRuntimes
            );

            tutorialFlowManager.StartTutorialFlow();
        }
        else
        {
            // --- 通常フローを開始 ---
            normalPhaseManager.gameObject.SetActive(true);
            tutorialFlowManager.gameObject.SetActive(false);
            normalPhaseManager.Init(
                entitiesManager,
                entitiesManager.Players,
                entitiesManager.Enemies,
                entitiesManager.PlayerStatusUIs,
                entitiesManager.EnemyStatusUIs,
                selectTurn,
                playerTurn,
                enemyTurn,
                selectionChoicePanel,
                this,
                battleCardDeck,
                this.guardGaugeSystem
            );

            normalPhaseManager.StartSelectionPhase();
        }
    }

    public void OnEnemyDead(EnemyRuntime enemy)
    {
        if (IsBattleEnded) return;

        Debug.Log($"敵撃破: {enemy.EnemyModel.EnemyName}");

        if (enemyRuntimes.Contains(enemy))
        {
            enemyRuntimes.Remove(enemy);
        }

        if (selectTurn != null)
        {
            selectTurn.RemoveEnemyFromSelections(enemy.EnemyModel);
        }

        if (entitiesManager.EnemyControllers.TryGetValue(enemy.EnemyModel, out EnemyController controller))
        {
            StartCoroutine(controller.DeadSequence());
            var ui = entitiesManager.EnemyStatusUIs.FirstOrDefault(u => u.GetEnemyModel() == enemy.EnemyModel);
            if (ui != null) ui.gameObject.SetActive(false);
        }

        // 勝利判定
        bool allEnemiesDead = enemyRuntimes.All(e => e.CurrentHP <= 0);
        if (allEnemiesDead)
        {
            StartCoroutine(BattleWinProcess());
        }
    }

    public void OnPlayerDead(PlayerRuntime player)
    {
        if (IsBattleEnded) return;

        Debug.Log($"味方死亡: {player.PlayerModel.PlayerName}");

        if (entitiesManager.PlayerControllers.TryGetValue(player.PlayerModel, out PlayerController controller))
        {
            controller.PlayDeadAnimation();
        }

        // 敗北判定: 全てのプレイヤーのHPが0以下か？
        bool allPlayersDead = entitiesManager.Players.All(p => p.CurrentHP <= 0);
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

        // チュートリアルの場合は終了処理
        if (entitiesManager.IsTutorialMode)
        {
            SceneManager.LoadScene("HomeScene");
            yield break;
        }
        // 現在のステージをクリアしたので、セーブデータを更新する
        StageManager.OnStageCleared(StageManager.SelectedStageID);
        StageManager.IsPostBattle = true;
        SceneManager.LoadScene("ScenarioScene");
    }

    private IEnumerator BattleLoseProcess()
    {
        IsBattleEnded = true;
        yield return new WaitForSeconds(1.5f);
        if (gameOverUIPanel != null)
        {
            gameOverUIPanel.SetActive(true);
        }

        yield return new WaitForSeconds(3.0f);
        SceneManager.LoadScene("HomeScene");
    }

    public IEnumerator StartPlayerTurnWithTimer(string phaseName = "Player Phase")
    {
        if (IsBattleEnded) yield break;

        yield return StartCoroutine(normalPhaseManager.ShowPhaseUI(phaseName));
        Debug.Log("【カード選択ターン開始】");
        timeText.enabled = true;

        playerTurn.Setup(
            selectTurn.PlayerSelections,
            entitiesManager.Players,
            battleCardDeck,
            entitiesManager.EnemyStatusUIs,
            entitiesManager.PlayerStatusUIs,
            entitiesManager.EnemyControllers,
            enemyRuntimes
        );

        playerTurn.StartPlayerTurn();

        turnTime = playerTurnDuration;
        float soundTime = 1f;
        while (turnTime >= 0 && !playerTurn.isTurnFinished && !IsBattleEnded)
        {
            if (soundTime <= 0f)
            {
                SoundManager.Instance.PlaySE(SEType.CountDown);
                soundTime = 1f;
            }
            turnTime -= Time.deltaTime;
            soundTime -= Time.deltaTime;
            timeText.text = turnTime.ToString("f2") + " <size=70%>SECOND</size>";
            yield return null;
        }
        turnTime = 0f;
        timeText.text = turnTime.ToString("f2") + " <size=70%>SECOND</size>";
        if (!IsBattleEnded)
        {
            playerTurn.FinishPlayerTurn();
        }
    }
}
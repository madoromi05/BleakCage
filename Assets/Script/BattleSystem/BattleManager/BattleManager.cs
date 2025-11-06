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
    [SerializeField] public BattleCardDeck battleCardDeck; // デッキはここに残す
    [SerializeField] private BattlePhaseManager normalPhaseManager; // ターン進行の核
    [SerializeField] private TutorialFlowManager tutorialFlowManager; // チュートリアルフロー管理
    [SerializeField] private BattleEntitiesManager entitiesManager; // エンティティ管理
    [SerializeField] public GuardGaugeSystem guardGaugeSystem; // ゲージシステム
    [SerializeField] private DefenseFeedbackUI defenseFeedbackUI; // フィードバックUI
    [SerializeField] private PlayerTurn playerTurn;
    [SerializeField] private EnemyTurn enemyTurn;
    [SerializeField] private SelectTurn selectTurn;

    [Header("UI関連")]
    [SerializeField] private Text timeText;
    [SerializeField] public GameObject selectionChoicePanel;
    [SerializeField] private Button keepSelectionsButton;
    [SerializeField] private Button changeSelectionsButton;
    [SerializeField] private GameObject targetMarkerPrefab;

    // --- ゲージ・フィードバック関連の Public メソッド (古い BattleManager のインターフェースを維持するためのラッパー) ---
    public bool TrySpendGuardGauge(float amount) => guardGaugeSystem.TrySpendGuardGauge(amount);
    public void AddGuardGauge(float amount) => guardGaugeSystem.AddGuardGauge(amount);
    public void IncrementCounterCount() => guardGaugeSystem.IncrementCounterCount();
    public void ShowDefenseFeedback(string message, Color color) => defenseFeedbackUI.ShowDefenseFeedback(message, color);

    // --- マーカー表示 (EntitiesManagerへの委譲) ---
    public void ShowTargetMarkerOnPlayer(int playerIndex) => entitiesManager.ShowTargetMarkerOnPlayer(MarkerInstance, playerIndex);
    public void HideTargetMarker() => entitiesManager.HideTargetMarker(MarkerInstance);

    // --- UIコールバック ---
    public void OnKeepSelections() => normalPhaseManager.OnKeepSelections();
    public void OnChangeSelections() => normalPhaseManager.OnChangeSelections();

    public GameObject MarkerInstance { get; private set; } // マーカーインスタンスを公開
    private float playerTurnDuration = 10f; // PhaseManager に移動しても良い
    private float turnTime = 10f; // PlayerTurnWithTimer で使用
    private Coroutine feedbackCoroutine;

    void Start()
    {
        // 1. UIとインスタンスの初期化
        if (targetMarkerPrefab != null)
        {
            MarkerInstance = Instantiate(targetMarkerPrefab, Vector3.zero, Quaternion.identity, this.transform);
            MarkerInstance.SetActive(false);
        }

        selectionChoicePanel.SetActive(false);

        // 2. システムの初期化
        guardGaugeSystem.Init();
        entitiesManager.Setup();

        // 3. コアロジックの初期化と委譲
        InitializeBattleFlow();
    }

    /// <summary>
    /// 通常モードかチュートリアルモードに応じて、
    /// 起動するフローマネージャーを切り替える
    /// </summary>
    private void InitializeBattleFlow()
    {
        // === 共通のセットアップ (PlayerTurn, EnemyTurn) ===
        List<CardRuntime> allCardsForDeck = new PlayerDataLoader().LoadPlayerPartyAndCards().AllCards;
        battleCardDeck.InitFromCardList(allCardsForDeck);

        List<PlayerModel> playerModels = entitiesManager.Players.Select(p => p.PlayerModel).ToList();
        enemyTurn.EnemySetup(playerModels, entitiesManager.Enemies, entitiesManager.EnemyControllers, entitiesManager.PlayerControllers, entitiesManager.PlayerStatusUIs);

        // === フローの分岐 ===
        bool isTutorial = entitiesManager.IsTutorialMode; // チュートリアル判定

        if (isTutorial)
        {
            // --- チュートリアルフローを開始 ---
            normalPhaseManager.gameObject.SetActive(false);
            tutorialFlowManager.gameObject.SetActive(true);

            // TutorialFlowManager に必要な依存関係をすべて渡して初期化
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
                battleCardDeck
            );

            tutorialFlowManager.StartTutorialFlow();
        }
        else
        {
            // --- 通常フローを開始 ---
            normalPhaseManager.gameObject.SetActive(true);
            tutorialFlowManager.gameObject.SetActive(false);

            // NormalPhaseManager を初期化 (引数が少し変わる可能性あり)
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
    // --- PlayerTurnWithTimer (PhaseManagerから呼ばれるため残すか、PhaseManagerに移動) ---
    // PlayerTurnWithTimer は BattleManager に残し、PhaseManager から Coroutine の開始を依頼する
    public IEnumerator StartPlayerTurnWithTimer(string phaseName = "Player Phase")
    {
        yield return StartCoroutine(normalPhaseManager.ShowPhaseUI(phaseName));
        Debug.Log("【カード選択ターン開始】");
        timeText.enabled = true;

        playerTurn.Setup(
            selectTurn.PlayerSelections,
            battleCardDeck,
            entitiesManager.EnemyStatusUIs,
            entitiesManager.EnemyControllers
        );

        playerTurn.StartPlayerTurn();

        turnTime = playerTurnDuration;
        while (turnTime >= 0 && !playerTurn.isTurnFinished)
        {
            turnTime -= Time.deltaTime;
            timeText.text = turnTime.ToString("f2") + " <size=70%>SECOND</size>";
            yield return null;
        }
        playerTurn.FinishPlayerTurn();
    }
}
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
        if (targetMarkerPrefab != null)
        {
            MarkerInstance = Instantiate(targetMarkerPrefab, Vector3.zero, Quaternion.identity, this.transform);
            MarkerInstance.SetActive(false);
        }

        selectionChoicePanel.SetActive(false);

        guardGaugeSystem.Init();
        entitiesManager.Setup();

        InitializeBattleFlow();
    }

    /// <summary>
    /// 通常モードかチュートリアルモードに応じて、
    /// 起動するフローマネージャーを切り替える
    /// </summary>
    private void InitializeBattleFlow()
    {
        List<CardRuntime> allCardsForDeck = new PlayerDataLoader().LoadPlayerPartyAndCards().AllCards;
        battleCardDeck.InitFromCardList(allCardsForDeck);

        List<PlayerModel> playerModels = entitiesManager.Players.Select(p => p.PlayerModel).ToList();
        enemyTurn.EnemySetup(playerModels, entitiesManager.Enemies, entitiesManager.EnemyControllers, entitiesManager.PlayerControllers, entitiesManager.PlayerStatusUIs);

        // === フローの分岐 ===
        bool isTutorial = entitiesManager.IsTutorialMode;

        if (isTutorial)
        {
            // --- チュートリアルフローを開始 ---
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
        float soundTime = 1f;
        while (turnTime >= 0 && !playerTurn.isTurnFinished)
        {
            if(soundTime <= 0f)
            {
                SoundManager.Instance.PlaySE(SEType.CountDown);
                soundTime = 1f;
            }
            turnTime -= Time.deltaTime;
            soundTime -= Time.deltaTime;
            timeText.text = turnTime.ToString("f2") + " <size=70%>SECOND</size>";
            yield return null;
        }
        playerTurn.FinishPlayerTurn();
    }
}
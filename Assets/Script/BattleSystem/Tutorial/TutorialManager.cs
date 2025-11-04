using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

#if TUTORIAL_ENABLED

/// <summary>
/// カード選択フェーズ時のチュートリアル管理クラス
/// チュートリアル以外は、無効化される
/// </summary>
public class TutorialManager : MonoBehaviour, IPhase
{
    public event System.Action OnPhaseFinished;

    [Header("Component References")]
    [SerializeField] public GameObject tutorialUIPanel;
    [SerializeField] private PlayerTurn playerTurn;
    [SerializeField] private BattlePhaseManager phaseManager; // BattleManager から変更
    [SerializeField] private Text tutorialText;
    [SerializeField] private SelectTurn selectTurn;

    private TutorialInputReader inputReader;
    private List<int> tutorialTargetCards = new List<int>() { 0, 1 };
    private List<int> currentlySelectedCards = new List<int>();
    private List<EnemyStatusUIController> enemyStatusUIControllers;
    private Dictionary<EnemyModel, EnemyController> enemyControllers;
    private Queue<string> tutorialMessages;
    private bool hasTurnFinished = false;
    private bool canProceed = false;
    private bool hasConfirmedSelection = false;

    public void Initialize(TutorialInputReader ir, List<EnemyStatusUIController> eUIs, Dictionary<EnemyModel, EnemyController> enemyControllers, SelectTurn selectTurn)
    {
        this.inputReader = ir;
        this.enemyStatusUIControllers = eUIs;
        this.enemyControllers = enemyControllers;
        this.selectTurn = selectTurn; // SelectTurn への参照を取得

        // PlayerTurn の初期化に使うデータは PhaseManager 経由でセットアップ時に提供される前提

        if (inputReader != null)
        {
            inputReader.OnProceed += HandleProceedInput;
        }

        playerTurn.OnCardSelectedForTutorial += HandleCardSelectedForTutorial;
        playerTurn.OnConfirmSelectionForTutorial += HandleConfirmSelectionTutorial;
    }

    private void OnDisable()
    {
        if (inputReader != null)
        {
            inputReader.OnProceed -= HandleProceedInput;
        }
        if (playerTurn != null)
        {
            playerTurn.OnCardSelectedForTutorial -= HandleCardSelectedForTutorial;
            playerTurn.OnConfirmSelectionForTutorial -= HandleConfirmSelectionTutorial;
        }
    }

    public void StartPhase()
    {
        tutorialUIPanel.SetActive(true);
        // PlayerTurn のセットアップとモード切り替えは PhaseManager の OnSelectionPhaseFinished で既に行われている

        StartCoroutine(TutorialCoroutine());
    }

    private void HandleProceedInput()
    {
        canProceed = true;
    }

    private void HandleConfirmSelectionTutorial()
    {
        hasConfirmedSelection = true;
    }

    private void HandleCardSelectedForTutorial(int inputNumber, bool isSelected)
    {
        // 0, 1, 2 を受け取るため、ここでは index のチェックは不要
        if (isSelected)
        {
            if (!currentlySelectedCards.Contains(inputNumber))
            {
                currentlySelectedCards.Add(inputNumber);
            }
        }
        else
        {
            currentlySelectedCards.Remove(inputNumber);
        }
    }

    private IEnumerator TutorialCoroutine()
    {
        InitializeMessages();

        yield return StartCoroutine(PlayerTurnExplanationFlow());

        // カード選択フェーズ完了後、OnPhaseFinished を呼んで次のフェーズ(EnemyTurnTutorialManager)へ移行する
        // ※ FutureFeatureExplanation() と EnemyTurnFlow() は EnemyTurnTutorialManager のロジックと重複するため削除します

        yield return StartCoroutine(EndTutorial()); // 終了メッセージだけ表示
        OnPhaseFinished?.Invoke();
    }

    private void InitializeMessages()
    {
        tutorialMessages = new Queue<string>();
        tutorialMessages.Enqueue("このフェーズでは、制限時間内に可能な限り多くのスキルカードを選択します。");
        tutorialMessages.Enqueue("配られた3枚のスキルカードのうち、最低1枚、最大2枚を選択します。選択はテンキーで行うことができ、" +
            "選択が終わったらEnterキーで残ったカードを破棄します。\r\n選んだカードはスタックされていき、破棄されたカードは\"修復\"するまでデッキに戻りません。" +
            "破棄が終わったら、再び3枚のスキルカードが提示されるので、制限時間が続く限りこれを繰り返します。");
        tutorialMessages.Enqueue("まずはこのカード2つを選択してみてください。（カード番号0と1に対応）");
        tutorialMessages.Enqueue("Enterキーでカードの選択を確定し、次に進みましょう");
        tutorialMessages.Enqueue("ここからは好きなカードを選択してください。選択したらEnterキーで次に進み、可能な限り多くのスキルカードを選択し、多くのダメージが与えられるように頑張りましょう！");
        tutorialMessages.Enqueue("次のクリック入力後、制限時間が開始します。");
    }

    private IEnumerator PlayerTurnExplanationFlow()
    {
        // PlayerTurn の Setup は PhaseManager 側で事前に呼び出す

        // 1. 最初のメッセージを表示
        SetTutorialText(tutorialMessages.Dequeue());
        yield return new WaitUntil(() => canProceed);
        canProceed = false;

        // 2. GIF付きのメッセージを表示
        SetTutorialText(tutorialMessages.Dequeue());
        yield return new WaitUntil(() => canProceed);
        canProceed = false;

        // 3. まずはこのカード2つを選択してみてください。
        SetTutorialText(tutorialMessages.Dequeue());
        playerTurn.StartPlayerTurn(); // カードを表示
        yield return new WaitUntil(() => CardsSelectedProsess());

        // 4. Enterキーで次に進みましょう
        SetTutorialText(tutorialMessages.Dequeue());
        yield return new WaitUntil(() => hasConfirmedSelection);

        // 5. ここからは好きなカードを選択してください....
        SetTutorialText(tutorialMessages.Dequeue());
        yield return new WaitUntil(() => canProceed);
        canProceed = false;

        // 6.次の左クリック入力後、制限時間が開始します。
        SetTutorialText(tutorialMessages.Dequeue());
        yield return new WaitUntil(() => canProceed);
        canProceed = false;

        if (tutorialUIPanel != null)
        {
            tutorialUIPanel.SetActive(false);
        }

        playerTurn.OnTurnFinished += OnPlayerTurnFinished;
        // PhaseManager 経由で BattleManager のコルーチンを開始
        phaseManager.StartCoroutine(phaseManager.StartPlayerTurnCoroutine("Player Phase"));
        yield return new WaitUntil(() => hasTurnFinished);
        playerTurn.OnTurnFinished -= OnPlayerTurnFinished;
    }

    private void SetTutorialText(string text)
    {
        tutorialText.text = text;
    }

    private void OnPlayerTurnFinished()
    {
        Debug.Log("[OnPlayerTurnFinished] 発火");
        hasTurnFinished = true;
    }

    /// <summary>
    /// カードが二枚選択されたかどうかの処理
    /// </summary>
    private bool CardsSelectedProsess()
    {
        return currentlySelectedCards.Count == tutorialTargetCards.Count && currentlySelectedCards.All(tutorialTargetCards.Contains);
    }

    private IEnumerator EndTutorial()
    {
        // チュートリアル終了後のメッセージ
        SetTutorialText("カード選択フェーズが終了しました。\nクリックで次のフェーズに移行します。");
        tutorialUIPanel.SetActive(true);
        yield return new WaitUntil(() => canProceed);
        canProceed = false;

        tutorialUIPanel.SetActive(false);
        Debug.Log("カード選択チュートリアル完了");
    }
}
#endif
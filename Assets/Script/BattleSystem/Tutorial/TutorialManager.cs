#if TUTORIAL_ENABLED


using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private GameObject tutorialUIPanel;
    [SerializeField] private TextMeshProUGUI tutorialText;
    [SerializeField] private RawImage tutorialGifImage;
    [SerializeField] private GifView gifView;

    private TortrialInputReader inputReader;

    private BattleManager battleManager;
    private PlayerTurn playerTurn;
    private EnemyTurn enemyTurn;

    private Queue<string> tutorialMessages;
    private List<int> tutorialTargetCards = new List<int>() { 0, 1 };
    private List<int> currentlySelectedCards = new List<int>();
    private bool hasTurnFinished = false;
    private bool canProceed = false;
    private bool hasConfirmedSelection = false;

    public void StartTutorialFlow(BattleManager bm, PlayerTurn pt, EnemyTurn et, TortrialInputReader ir)
    {
        this.battleManager = bm;
        this.playerTurn = pt;
        this.enemyTurn = et;
        this.inputReader = ir;

        if (inputReader != null)
        {
            inputReader.OnProceed += HandleProceedInput;
        }

        playerTurn.OnCardSelectedForTutorial += HandleCardSelectedForTutorial;
        playerTurn.OnConfirmSelectionForTutorial += HandleConfirmSelectionTutorial;
        gifView.Initialize(tutorialGifImage);
        tutorialUIPanel.SetActive(true);
        StartCoroutine(TutorialCoroutine());
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
        tutorialUIPanel.SetActive(true);
        InitializeMessages();

        yield return StartCoroutine(PlayerTurnExplanationFlow());
        yield return StartCoroutine(FutureFeatureExplanation());
        yield return StartCoroutine(EnemyTurnFlow());
        yield return StartCoroutine(EndTutorial());
    }

    private void InitializeMessages()
    {
        tutorialMessages = new Queue<string>();
        tutorialMessages.Enqueue("このフェーズでは、制限時間内に可能な限り多くのスキルカードを選択します。");
        tutorialMessages.Enqueue("");
        //tutorialMessages.Enqueue("<gif：説明動画>\r\n文章：配られた3枚のスキルカードのうち、最低1枚、最大2枚を選択します。選択はテンキーで行うことができ、" +
        //    "選択が終わったらEnterキーで残ったカードを破棄します。\r\n選んだカードはスタックされていき、破棄されたカードは\"修復\"するまでデッキに戻りません。" +
        //    "\r\n破棄が終わったら、再び3枚のスキルカードが提示されるので、制限時間が続く限りこれを繰り返します。");
        tutorialMessages.Enqueue("まずはこのカード2つを選択してみてください。");
        tutorialMessages.Enqueue("Enterキーで次に進みましょう");
        tutorialMessages.Enqueue("ここからは好きなカードを選択してください。選択したらEnterキーで次に進み、可能な限り多くのスキルカードを選択し、多くのダメージが与えられるように頑張りましょう！");
        tutorialMessages.Enqueue("次の左クリック入力後、制限時間が開始します。");
    }

    private IEnumerator PlayerTurnExplanationFlow()
    {
        playerTurn.SetTutorialMode(true);
        battleManager.StartPlayerTurnForTutorial();

        // 1. 最初のメッセージを表示
        SetTutorialText(tutorialMessages.Dequeue());
        yield return new WaitUntil(() => canProceed);
        canProceed = false;

        // 2. GIF付きのメッセージを表示
        SetTutorialTextAndGif(tutorialMessages.Dequeue(), "test.gif");
        yield return new WaitUntil(() => canProceed);
        canProceed = false;
        gifView.StopGif();

        // 3. まずはこのカード2つを選択してみてください。
        SetTutorialText(tutorialMessages.Dequeue());

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

        tutorialUIPanel.SetActive(false);
        playerTurn.SetTutorialMode(false);

        playerTurn.OnTurnFinished += OnPlayerTurnFinished;
        battleManager.StartCoroutine(battleManager.StartPlayerTurnWithTimer());
        yield return new WaitUntil(() => hasTurnFinished);
        playerTurn.OnTurnFinished -= OnPlayerTurnFinished;
    }

    // メッセージ設定用の補助関数
    private void SetTutorialText(string text)
    {
        tutorialText.text = text;
        gifView.StopGif();
    }

    // GIF付きメッセージ設定用の補助関数
    private void SetTutorialTextAndGif(string text, string gifFileName)
    {
        tutorialText.text = text;
        StartCoroutine(gifView.LoadAndPlayGif(gifFileName));
    }
    private void OnPlayerTurnFinished()
    {
        hasTurnFinished = true;
    }

    /// <summary>
    /// カードが二枚選択されたかどうかの処理
    /// </summary>
    private bool CardsSelectedProsess()
    {
        return currentlySelectedCards.Count == tutorialTargetCards.Count && currentlySelectedCards.All(tutorialTargetCards.Contains);
    }

    private IEnumerator FutureFeatureExplanation()
    {
        SetTutorialText("（将来的にはここで、攻撃するキャラクターの優先順位を決めます）");
        yield return new WaitUntil(() => canProceed);
        canProceed = false;
    }

    private IEnumerator EnemyTurnFlow()
    {
        SetTutorialText("次は敵のターンです。");
        yield return new WaitUntil(() => canProceed);
        canProceed = false;

        enemyTurn.StartEnemyTurn();
        SetTutorialText("敵が攻撃してきます！防御の準備を！");
        yield return new WaitUntil(() => canProceed);
        canProceed = false;
    }

    private IEnumerator EndTutorial()
    {
        tutorialText.text = "敵のターンが終了しました。\nこれでチュートリアルは終わりです。";
        yield return new WaitUntil(() => canProceed);
        canProceed = false;

        tutorialUIPanel.SetActive(false);
        Debug.Log("チュートリアル完了");
    }
}

#endif
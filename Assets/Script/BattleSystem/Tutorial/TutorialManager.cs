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
    private bool canProceed = false;

    private BattleManager battleManager;
    private PlayerTurn playerTurn;
    private EnemyTurn enemyTurn;

    private Queue<string> tutorialMessages;
    private List<int> tutorialTargetCards = new List<int>() { 0, 1 };
    private List<int> currentlySelectedCards = new List<int>();
    private bool hasTurnFinished = false;

    private void Awake()
    {
        gifView.Initialize(tutorialGifImage);
    }

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
        }
    }

    private void HandleProceedInput()
    {
        canProceed = true;
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
        tutorialMessages.Enqueue("<gif：説明動画>\r\n文章：配られた3枚のスキルカードのうち、最低1枚、最大2枚を選択します。選択はテンキーで行うことができ、" +
            "選択が終わったらEnterキーで残ったカードを破棄します。\r\n選んだカードはスタックされていき、破棄されたカードは\"修復\"するまでデッキに戻りません。" +
            "\r\n破棄が終わったら、再び3枚のスキルカードが提示されるので、制限時間が続く限りこれを繰り返します。");
        tutorialMessages.Enqueue("まずはこのカード2つを選択してみてください。");
        tutorialMessages.Enqueue("ここからは好きなカードを選択してください。選択したらEnterキーで次に進み、可能な限り多くのスキルカードを選択し、多くのダメージが与えられるように頑張りましょう！");
    }

    private IEnumerator PlayerTurnExplanationFlow()
    {
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

        // 3. カード選択の説明メッセージを表示
        SetTutorialText(tutorialMessages.Dequeue());
        yield return new WaitForSeconds(0.5f);

        playerTurn.SetTutorialMode(true);
        playerTurn.DrawHandCards();

        yield return new WaitUntil(() => CorrectCardsSelected());

        // 4. 正しいカード選択後のメッセージ
        SetTutorialText("素晴らしい！\nEnterキーで次に進みましょう。");
        yield return new WaitUntil(() => canProceed);
        canProceed = false;

        // 5. 好きなカード選択のメッセージ
        SetTutorialText(tutorialMessages.Dequeue() + "「次のEnterキー入力後、制限時間が開始します。」");
        playerTurn.SetTutorialMode(false);
        yield return new WaitUntil(() => canProceed);
        canProceed = false;

        playerTurn.OnTurnFinished += OnPlayerTurnFinished;
        battleManager.StartCoroutine(battleManager.StartPlayerTurnWithTimer());
        yield return new WaitUntil(() => hasTurnFinished);
        playerTurn.OnTurnFinished -= OnPlayerTurnFinished;

        Debug.Log("プレイヤーのターン終了。");
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

    private bool CorrectCardsSelected()
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
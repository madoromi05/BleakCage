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
    [SerializeField] public GameObject TutorialUIPanel;
    [SerializeField] private PlayerTurn _playerTurn;
    [SerializeField] private BattleManager _battleManager;
    [SerializeField] private Text _tutorialText;
    [SerializeField] private SelectTurn _selectTurn;

    private TutorialInputReader _inputReader;
    private List<int> _tutorialTargetCards = new List<int>() { 0, 1 };
    private List<int> _currentlySelectedCards = new List<int>();
    private List<EnemyStatusUIController> _enemyStatusUIControllers;
    private Dictionary<EnemyModel, EnemyController> _enemyControllers;
    private Queue<string> _tutorialMessages;
    private bool _hasTurnFinished = false;
    private bool _canProceed = false;
    private bool _hasConfirmedSelection = false;

    public void Initialize(BattleManager bm, TutorialInputReader ir, List<EnemyStatusUIController> eUIs, Dictionary<EnemyModel, EnemyController> enemyControllers, SelectTurn selectTurn)
    {
        _battleManager = bm;
        _inputReader = ir;
        _enemyStatusUIControllers = eUIs;
        _enemyControllers = enemyControllers;
        _selectTurn = selectTurn; // SelectTurn への参照を取得

        // PlayerTurn の初期化に使うデータは PhaseManager 経由でセットアップ時に提供される前提

        if (_inputReader != null)
        {
            _inputReader.OnProceed += HandleProceedInput;
        }

        _playerTurn.OnCardSelectedForTutorial += HandleCardSelectedForTutorial;
        _playerTurn.OnConfirmSelectionForTutorial += HandleConfirmSelectionTutorial;
    }

    private void OnDisable()
    {
        if (_inputReader != null)
        {
            _inputReader.OnProceed -= HandleProceedInput;
        }
        if (_playerTurn != null)
        {
            _playerTurn.OnCardSelectedForTutorial -= HandleCardSelectedForTutorial;
            _playerTurn.OnConfirmSelectionForTutorial -= HandleConfirmSelectionTutorial;
        }
    }

    public void StartPhase()
    {
        TutorialUIPanel.SetActive(true);
        StartCoroutine(TutorialCoroutine());
    }

    private void HandleProceedInput()
    {
        _canProceed = true;
    }

    private void HandleConfirmSelectionTutorial()
    {
        _hasConfirmedSelection = true;
    }

    private void HandleCardSelectedForTutorial(int inputNumber, bool isSelected)
    {
        // 0, 1, 2 を受け取るため、ここでは index のチェックは不要
        if (isSelected)
        {
            if (!_currentlySelectedCards.Contains(inputNumber))
            {
                _currentlySelectedCards.Add(inputNumber);
            }
        }
        else
        {
            _currentlySelectedCards.Remove(inputNumber);
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
        _tutorialMessages = new Queue<string>();
        _tutorialMessages.Enqueue("このフェーズでは、制限時間内に可能な限り多くのスキルカードを選択します。");
        _tutorialMessages.Enqueue("配られた3枚のスキルカードのうち、最低1枚、最大2枚を選択します。選択はテンキーで行うことができ、" +
            "選択が終わったらEnterキーで残ったカードを破棄します。\r\n選んだカードはスタックされていき、破棄されたカードは\"修復\"するまでデッキに戻りません。" +
            "破棄が終わったら、再び3枚のスキルカードが提示されるので、制限時間が続く限りこれを繰り返します。");
        _tutorialMessages.Enqueue("まずはこのカード2つを選択してみてください。（カード番号0と1に対応）");
        _tutorialMessages.Enqueue("Enterキーでカードの選択を確定し、次に進みましょう");
        _tutorialMessages.Enqueue("ここからは好きなカードを選択してください。選択したらEnterキーで次に進み、可能な限り多くのスキルカードを選択し、多くのダメージが与えられるように頑張りましょう！");
        _tutorialMessages.Enqueue("次のクリック入力後、制限時間が開始します。");
    }

    private IEnumerator PlayerTurnExplanationFlow()
    {
        // 1. 最初のメッセージを表示
        SetTutorialText(_tutorialMessages.Dequeue());
        yield return new WaitUntil(() => _canProceed);
        _canProceed = false;

        // 2. GIF付きのメッセージを表示
        SetTutorialText(_tutorialMessages.Dequeue());
        yield return new WaitUntil(() => _canProceed);
        _canProceed = false;

        // 3. まずはこのカード2つを選択してみてください。
        SetTutorialText(_tutorialMessages.Dequeue());
        _playerTurn.StartPlayerTurn(); // カードを表示
        yield return new WaitUntil(() => CardsSelectedProsess());

        // 4. Enterキーで次に進みましょう
        SetTutorialText(_tutorialMessages.Dequeue());
        yield return new WaitUntil(() => _hasConfirmedSelection);

        // 5. ここからは好きなカードを選択してください....
        SetTutorialText(_tutorialMessages.Dequeue());
        yield return new WaitUntil(() => _canProceed);
        _canProceed = false;

        // 6.次の左クリック入力後、制限時間が開始します。
        SetTutorialText(_tutorialMessages.Dequeue());
        yield return new WaitUntil(() => _canProceed);
        _canProceed = false;

        if (TutorialUIPanel != null)
        {
            TutorialUIPanel.SetActive(false);
        }

        _playerTurn.OnTurnFinished += OnPlayerTurnFinished;
        _battleManager.StartCoroutine(_battleManager.StartPlayerTurnWithTimer("Player Phase"));
        _playerTurn.OnTurnFinished -= OnPlayerTurnFinished;
    }

    private void SetTutorialText(string text)
    {
        _tutorialText.text = text;
    }

    private void OnPlayerTurnFinished()
    {
        Debug.Log("[OnPlayerTurnFinished] 発火");
        _hasTurnFinished = true;
    }

    /// <summary>
    /// カードが二枚選択されたかどうかの処理
    /// </summary>
    private bool CardsSelectedProsess()
    {
        return _currentlySelectedCards.Count == _tutorialTargetCards.Count && _currentlySelectedCards.All(_tutorialTargetCards.Contains);
    }

    private IEnumerator EndTutorial()
    {
        // チュートリアル終了後のメッセージ
        SetTutorialText("カード選択フェーズが終了しました。\nクリックで次のフェーズに移行します。");
        TutorialUIPanel.SetActive(true);
        yield return new WaitUntil(() => _canProceed);
        _canProceed = false;

        TutorialUIPanel.SetActive(false);
        Debug.Log("カード選択チュートリアル完了");
    }
}
#endif
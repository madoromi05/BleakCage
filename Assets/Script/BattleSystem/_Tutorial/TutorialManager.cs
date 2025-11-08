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
    [SerializeField] private BattleInputReader _battleInputReader;

    private TutorialInputReader _inputReader;
    private List<int> _tutorialTargetCards = new List<int>() { 0, 1 };
    private List<int> _currentlySelectedCards = new List<int>();
    private List<EnemyStatusUIController> _enemyStatusUIControllers;
    private Dictionary<EnemyModel, EnemyController> _enemyControllers;
    private Queue<string> _tutorialMessages;
    private bool _hasTurnFinished = false;
    private bool _canProceed = false;
    private bool _hasConfirmedSelection = false;

    public void Initialize(BattleManager battleManager, TutorialInputReader inputReader, 
        List<EnemyStatusUIController> enemyStatusUIControllers, Dictionary<EnemyModel, EnemyController> enemyControllers, SelectTurn selectTurn)
    {
        _battleManager = battleManager;
        _inputReader = inputReader;
        _enemyStatusUIControllers = enemyStatusUIControllers;
        _enemyControllers = enemyControllers;
        _selectTurn = selectTurn;

        if (_inputReader != null)
        {
            _inputReader.OnProceed += HandleProceedInput;
        }
        if (_battleInputReader == null)
        {
            Debug.LogError("BattleInputReader が TutorialManager にアサインされていません！");
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
        _battleInputReader?.DisableAllActionMaps();
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

    /// <summary>
    /// カード選択チュートリアル開始時のコルーチン
    /// </summary>
    /// <returns></returns>
    private IEnumerator TutorialCoroutine()
    {
        InitializeMessages();
        _battleInputReader?.DisableAllActionMaps();
        yield return StartCoroutine(PlayerTurnExplanationFlow());
        yield return StartCoroutine(EndTutorial());
        OnPhaseFinished?.Invoke();
    }

    private void InitializeMessages()
    {
        _tutorialMessages = new Queue<string>();
        _tutorialMessages.Enqueue("このフェーズでは、制限時間内に可能な限り多くのスキルカードを選択します。");
        _tutorialMessages.Enqueue("配られた3枚のスキルカードのうち、最低1枚、最大2枚を選択します。選択はテンキーで行い、" +
            "選択が終わったらEnterキーで残りのカードを破棄します。\r\n選んだカードはスタックされていき、破棄されたカードは次のターンまでデッキに戻りません。" +
            "破棄が終わったら、再び選択し、制限時間が続く限りこれを繰り返します。");
        _tutorialMessages.Enqueue("まずはこのカード2つを選択してみてください。（カード番号0と1に対応）");
        _tutorialMessages.Enqueue("Enterキーでカードの選択を確定し、次に進みましょう");
        _tutorialMessages.Enqueue("ここからは好きなカードを選択してください。選択したらEnterキーで次に進み、可能な限り多くのスキルカードを選択し、多くのダメージが与えられるように頑張りましょう！");
        _tutorialMessages.Enqueue("次のクリック入力後、制限時間が開始します。");
    }

    private IEnumerator PlayerTurnExplanationFlow()
    {
        SetTutorialText(_tutorialMessages.Dequeue());
        yield return new WaitUntil(() => _canProceed);
        _canProceed = false;

        SetTutorialText(_tutorialMessages.Dequeue());
        yield return new WaitUntil(() => _canProceed);
        _canProceed = false;

        _battleInputReader.EnableBattleActionMap();
        Debug.Log("BattleInputReader enabled for tutorial card selection.");

        SetTutorialText(_tutorialMessages.Dequeue());
        _playerTurn.StartPlayerTurn(); // カードを表示

        // WaitUntil: カード0と1が選択されるのを待つ
        yield return new WaitUntil(() => CardsSelectedProsess());
        Debug.Log("Tutorial cards (0 and 1) selected.");

        // 4. Enterキーで次に進む
        SetTutorialText(_tutorialMessages.Dequeue());

        // WaitUntil: Enterキー (Confirm) が押されるのを待つ
        yield return new WaitUntil(() => _hasConfirmedSelection);
        _hasConfirmedSelection = false;
        Debug.Log("Tutorial card confirmation received.");

        _battleInputReader.DisableAllActionMaps();
        SetTutorialText(_tutorialMessages.Dequeue());
        yield return new WaitUntil(() => _canProceed);
        _canProceed = false;

        SetTutorialText(_tutorialMessages.Dequeue());
        yield return new WaitUntil(() => _canProceed);
        _canProceed = false;

        if (TutorialUIPanel != null)
        {
            TutorialUIPanel.SetActive(false);
        }

        _battleInputReader.EnableBattleActionMap();

        _hasTurnFinished = false;
        _playerTurn.OnTurnFinished += OnPlayerTurnFinished;

        _battleManager.StartCoroutine(_battleManager.StartPlayerTurnWithTimer("Player Phase"));

        yield return new WaitUntil(() => _hasTurnFinished);

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
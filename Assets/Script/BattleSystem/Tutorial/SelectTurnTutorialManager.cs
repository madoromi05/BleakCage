/// <summary>
/// 攻撃対象選択フェーズ時のチュートリアル管理クラス
/// チュートリアル以外は、無効化される
/// </summary>
#if TUTORIAL_ENABLED
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Experimental.GraphView.GraphView;

public class SelectTurnTutorialManager : MonoBehaviour
{
    [SerializeField] private GameObject tutorialUIPanel;
    [SerializeField] private TextMeshProUGUI tutorialText;
    [SerializeField] private RawImage tutorialGifImage;
    [SerializeField] private GifViewController gifView;

    private TortrialInputReader _inputReader;
    private SelectTurn _selectTurn;
    private List<PlayerRuntime> _currentParty;
    private List<EnemyModel> _currentEnemies;
    private List<PlayerStatusUIController> _playerUIs;
    private List<EnemyStatusUIController> _enemyUIs;

    private Queue<string> tutorialMessages;
    private bool canProceed = false;
    private bool hasSelectionConfirmed = false;
    private int requiredSelections = 0;
    private int currentSelections = 0;

    public event System.Action OnSelectTurnTutorialFinished;

    public void StartTutorialFlow(SelectTurn st, TortrialInputReader ir, List<PlayerRuntime> players, List<EnemyModel> enemies, List<PlayerStatusUIController> pUIs, List<EnemyStatusUIController> eUIs)
    {
        _selectTurn = st;
        _inputReader = ir;
        _currentParty = players;
        _currentEnemies = enemies;
        _playerUIs = pUIs;
        _enemyUIs = eUIs;

        if (_inputReader != null)
        {
            _inputReader.OnProceed += HandleProceedInput;
        }

        _selectTurn.OnTargetSelectedForTutorial += HandleTargetSelectedForTutorial;
        _selectTurn.StartSelectTurn(players, enemies, pUIs, eUIs);

        gifView.Initialize(tutorialGifImage);
        tutorialUIPanel.SetActive(true);
        StartCoroutine(TutorialCoroutine());
    }

    private void OnDisable()
    {
        if (_inputReader != null)
        {
            _inputReader.OnProceed -= HandleProceedInput;
        }
        if (_selectTurn != null)
        {
            _selectTurn.OnTargetSelectedForTutorial -= HandleTargetSelectedForTutorial;
        }
    }

    private void HandleProceedInput()
    {
        canProceed = true;
    }

    // SelectTurn側でターゲットが選択されたら呼び出される
    private void HandleTargetSelectedForTutorial()
    {
        currentSelections++;
        if (currentSelections >= requiredSelections)
        {
            hasSelectionConfirmed = true;
        }
    }

    private IEnumerator TutorialCoroutine()
    {
        InitializeMessages();
        _selectTurn.SetTutorialMode(true); // SelectTurnをチュートリアルモードに設定
        _selectTurn.InitializeForTutorial(_currentParty, _currentEnemies, _playerUIs, _enemyUIs);

        // 1. 最初のメッセージ
        SetTutorialText(tutorialMessages.Dequeue());
        yield return new WaitUntil(() => canProceed);
        canProceed = false;

        // 2. GIF付きの説明
        SetTutorialTextAndGif(tutorialMessages.Dequeue(), "select_target.gif"); // 仮のGIF名
        yield return new WaitUntil(() => canProceed);
        canProceed = false;
        gifView.StopGif();

        // 3. 実際の選択を促す
        SetTutorialText(tutorialMessages.Dequeue());

        // プレイヤー1人目、最初のターゲット選択を待つ
        requiredSelections = 1;
        currentSelections = 0;
        hasSelectionConfirmed = false;
        // チュートリアルモードで選択プロセスを開始（プレイヤー1人分だけ実行）
        StartCoroutine(_selectTurn.SelectionProcessCoroutineForTutorial(1));

        yield return new WaitUntil(() => hasSelectionConfirmed);

        // 4. 完了メッセージ
        SetTutorialText(tutorialMessages.Dequeue());
        yield return new WaitUntil(() => canProceed);
        canProceed = false;

        // チュートリアル終了処理
        tutorialUIPanel.SetActive(false);
        _selectTurn.SetTutorialMode(false);

        // チュートリアルで選択されなかったデータを補完
        _selectTurn.FinalizeSelectionsForTutorial();

        // BattleManagerにチュートリアル完了を通知
        OnSelectTurnTutorialFinished?.Invoke();
    }

    private void InitializeMessages()
    {
        tutorialMessages = new Queue<string>();
        tutorialMessages.Enqueue("このフェーズでは、各キャラクターが攻撃する敵の優先順位を決めます。");
        tutorialMessages.Enqueue("<gif>矢印キーで攻撃したい敵を選択し、Enterキーで決定します。\nこれをキャラクターの人数分、優先順位の数だけ繰り返します。");
        tutorialMessages.Enqueue("まず、最初のキャラクターの第1優先ターゲットを選択してみましょう。\n矢印キーで敵を選び、Enterキーで決定してください。");
        tutorialMessages.Enqueue("うまく選択できましたね！\n実際のゲームでは、これを全キャラクター分行います。");
    }

    private void SetTutorialText(string text)
    {
        tutorialText.text = text;
        tutorialGifImage.gameObject.SetActive(false);
    }

    private void SetTutorialTextAndGif(string text, string gifFileName)
    {
        tutorialText.text = text;
        tutorialGifImage.gameObject.SetActive(true);
        StartCoroutine(gifView.LoadAndPlayGif(gifFileName));
    }
}
#endif
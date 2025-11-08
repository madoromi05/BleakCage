using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

#if TUTORIAL_ENABLED

/// <summary>
/// 攻撃対象選択ターンのチュートリアルを担当するクラス
/// </summary>
public class SelectTurnTutorialManager : MonoBehaviour, IPhase
{
    [Header("Component References")]
    [SerializeField] private GameObject _tutorialUIPanel;
    [SerializeField] private Text _tutorialText;
    [SerializeField] private SelectTurn _selectTurn;
    [SerializeField] private SelectInputReader _selectInputReader;

    public event System.Action OnPhaseFinished;

    private TutorialInputReader _inputReader;
    private List<PlayerRuntime> _currentParty;
    private List<EnemyModel> _currentEnemies;
    private List<PlayerStatusUIController> _playerUIs;
    private List<EnemyStatusUIController> _enemyUIs;

    private Queue<string> _tutorialMessages;
    private bool _canProceed = false;

    public void Initialize(TutorialInputReader inputReader, List<PlayerRuntime> players, List<EnemyModel> enemies,
        List<PlayerStatusUIController> playerUIs, List<EnemyStatusUIController> enemyUIs)
    {
        _inputReader = inputReader;
        _currentParty = players;
        _currentEnemies = enemies;
        _playerUIs = playerUIs;
        _enemyUIs = enemyUIs;
        _selectTurn.Initialize(players, enemies, playerUIs, enemyUIs);
    }

    public void StartPhase()
    {
        Debug.Log("Starting Select Turn Tutorial Phase");
        if (_inputReader != null)
        {
            _inputReader.OnProceed += HandleProceedInput;
        }

        _tutorialUIPanel.SetActive(true);
        StartCoroutine(TutorialCoroutine());
    }

    private void OnDisable()
    {
        if (_inputReader != null)
        {
            _inputReader.OnProceed -= HandleProceedInput;
        }
        _selectInputReader?.DisableActionMap();
    }


    private void HandleProceedInput()
    {
        _canProceed = true;
    }

    /// <summary>
    /// SelectTurnチュートリアルの文章を順に表示し、実践を交えるコルーチン
    /// </summary>
    /// <returns></returns>
    private IEnumerator TutorialCoroutine()
    {
        InitializeMessages();

        SetTutorialText(_tutorialMessages.Dequeue());
        yield return new WaitUntil(() => _canProceed);
        _canProceed = false;

        SetTutorialText(_tutorialMessages.Dequeue());
        yield return new WaitUntil(() => _canProceed);
        _canProceed = false;


        if (_selectInputReader == null)
        {
            Debug.LogError("SelectInputReader が SelectTurnTutorialManager にアサインされていません！");
            yield break;
        }
        //敵選択の実践
        _selectInputReader.EnableActionMap();
        SetTutorialText(_tutorialMessages.Dequeue());

        PlayerRuntime tutorialPlayer = _currentParty[0];
        PlayerStatusUIController tutorialPlayerUI = _playerUIs[0];
        tutorialPlayerUI.SetHighlight(new Color(0.5f, 0.8f, 1f));

        // SelectTurnクラスの「1体選択コルーチン」を呼び出し、終わるまで待機
        yield return StartCoroutine(_selectTurn.SelectOneTargetCoroutine(tutorialPlayer, 1, (selectedEnemy) => {
            Debug.Log($"Tutorial selection complete: {selectedEnemy.EnemyName}");
        }));

        tutorialPlayerUI.ResetHighlight();
        _selectInputReader.DisableActionMap();

        SetTutorialText(_tutorialMessages.Dequeue());
        yield return new WaitUntil(() => _canProceed);
        _canProceed = false;
        _selectTurn.FinalizeSelectionsForTutorial();
        OnPhaseFinished?.Invoke();
    }

    private void InitializeMessages()
    {
        _tutorialMessages = new Queue<string>();
        _tutorialMessages.Enqueue("このフェーズでは、各キャラクターが攻撃する敵の優先順位を決めます。（クリックで次へ）");
        _tutorialMessages.Enqueue("攻撃したい敵を選択し、Enterキーで決定します。\nこれをキャラクターの人数分、優先順位の数だけ繰り返します。（クリックで次へ）");
        _tutorialMessages.Enqueue("まず、最初のキャラクターの第1優先ターゲットを選択してみましょう。\n矢印(上・下)キーで敵を選び、Enterキーで決定してください。");
        _tutorialMessages.Enqueue("うまく選択できましたね！\n実際のゲームでは、これを全キャラクター分行います。\n（クリックで次へ）");
    }

    private void SetTutorialText(string text)
    {
        _tutorialText.text = text;
    }
}
#endif
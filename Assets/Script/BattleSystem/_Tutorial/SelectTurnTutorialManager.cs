using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if TUTORIAL_ENABLED

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
        if (_inputReader == null)
        {
            DebugCostom.LogError("【Critical】TutorialInputReader が null です！TutorialFlowManager の Inspector でアサインを確認してください。");
            return;
        }

        _inputReader.OnProceed += HandleProceedInput;

        if (_tutorialUIPanel != null)
        {
            _tutorialUIPanel.SetActive(true);
        }
        else
        {
            DebugCostom.LogError("TutorialUIPanel がアサインされていません！");
        }

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

    private IEnumerator TutorialCoroutine()
    {
        InitializeMessages();

        // 1メッセージ目
        if (_tutorialMessages.Count > 0)
        {
            SetTutorialText(_tutorialMessages.Dequeue());
            yield return new WaitUntil(() => _canProceed);
            _canProceed = false;
        }

        // 2メッセージ目
        if (_tutorialMessages.Count > 0)
        {
            SetTutorialText(_tutorialMessages.Dequeue());
            yield return new WaitUntil(() => _canProceed);
            _canProceed = false;
        }

        // 敵選択の実践
        if (_selectInputReader == null)
        {
            DebugCostom.LogError("SelectInputReader が SelectTurnTutorialManager にアサインされていません！");
            yield break;
        }

        _selectInputReader.EnableActionMap();

        if (_tutorialMessages.Count > 0)
            SetTutorialText(_tutorialMessages.Dequeue());

        // 安全策: プレイヤーデータがあるか確認
        if (_currentParty != null && _currentParty.Count > 0)
        {
            PlayerRuntime tutorialPlayer = _currentParty[0];
            PlayerStatusUIController tutorialPlayerUI = _playerUIs[0];
            tutorialPlayerUI.SetHighlight(new Color(0.5f, 0.8f, 1f));
            yield return StartCoroutine(_selectTurn.SelectOneTargetCoroutine(tutorialPlayer, 1, (selectedEnemy) => {
                DebugCostom.Log($"Tutorial selection complete: {selectedEnemy.EnemyName}");
            }));

            tutorialPlayerUI.ResetHighlight();
        }
        else
        {
            DebugCostom.LogError("チュートリアル用のプレイヤーデータが存在しません。");
        }

        _selectInputReader.DisableActionMap();

        // 3メッセージ目
        if (_tutorialMessages.Count > 0)
        {
            SetTutorialText(_tutorialMessages.Dequeue());
            yield return new WaitUntil(() => _canProceed);
            _canProceed = false;
        }

        _selectTurn.FinalizeSelectionsForTutorial();
        OnPhaseFinished?.Invoke();
    }
    private void InitializeMessages()
    {
        _tutorialMessages = new Queue<string>();
        _tutorialMessages.Enqueue("このフェーズでは、各キャラクターが攻撃する敵の優先順位を決めます。（左クリックで次へ）");
        _tutorialMessages.Enqueue("攻撃したい敵を選択し、Enterキーで決定します。\nこれをキャラクターの人数分、優先順位の数だけ繰り返します。（左クリックで次へ）");
        _tutorialMessages.Enqueue("まず、最初のキャラクターの第1優先ターゲットを選択してみましょう。\n矢印(上・下)キーで敵を選び、Enterキーで決定してください。");
        _tutorialMessages.Enqueue("うまく選択できましたね！\n実際のゲームでは、これを全キャラクター分行います。\n（左クリックで次へ）");
    }

    private void SetTutorialText(string text)
    {
        if (_tutorialText != null) _tutorialText.text = text;
    }
}
#endif
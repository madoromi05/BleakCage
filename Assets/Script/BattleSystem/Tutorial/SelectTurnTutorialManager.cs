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

    private TutorialInputReader _inputReader;
    private List<PlayerRuntime> _currentParty;
    private List<EnemyModel> _currentEnemies;
    private List<PlayerStatusUIController> _playerUIs;
    private List<EnemyStatusUIController> _enemyUIs;

    private Queue<string> _tutorialMessages;
    private bool _canProceed = false;

    public event System.Action OnPhaseFinished;

    public void Initialize(TutorialInputReader ir, List<PlayerRuntime> players, List<EnemyModel> enemies, List<PlayerStatusUIController> pUIs, List<EnemyStatusUIController> eUIs)
    {
        _inputReader = ir;
        _currentParty = players;
        _currentEnemies = enemies;
        _playerUIs = pUIs;
        _enemyUIs = eUIs;

        // SelectTurn の Initialize を呼び出す
        _selectTurn.Initialize(players, enemies, pUIs, eUIs);
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
    }


    private void HandleProceedInput()
    {
        _canProceed = true;
    }

    private IEnumerator TutorialCoroutine()
    {
        InitializeMessages();

        // 1. 最初のメッセージ
        SetTutorialText(_tutorialMessages.Dequeue());
        yield return new WaitUntil(() => _canProceed);
        _canProceed = false;

        // 2. GIF付きの説明
        SetTutorialText(_tutorialMessages.Dequeue());
        yield return new WaitUntil(() => _canProceed);
        _canProceed = false;

        // 3. 実際の選択を促す
        SetTutorialText(_tutorialMessages.Dequeue());

        if (_currentParty == null || _currentParty.Count == 0 || _playerUIs == null || _playerUIs.Count == 0)
        {
            Debug.LogError("チュートリアルを実行するプレイヤーが見つかりません。");
            yield break;
        }

        PlayerRuntime tutorialPlayer = _currentParty[0];
        PlayerStatusUIController tutorialPlayerUI = _playerUIs[0];

        // 該当プレイヤーのUIをハイライト
        tutorialPlayerUI.SetHighlight(new Color(0.5f, 0.8f, 1f));

        // SelectTurnクラスの「1体選択コルーチン」を呼び出し、終わるまで待機
        yield return StartCoroutine(_selectTurn.SelectOneTargetCoroutine(tutorialPlayer, 1, (selectedEnemy) => {
            Debug.Log($"Tutorial selection complete: {selectedEnemy.EnemyName}");
        }));

        // プレイヤーUIのハイライトを戻す
        tutorialPlayerUI.ResetHighlight();

        // 4. 完了メッセージ
        SetTutorialText(_tutorialMessages.Dequeue());
        yield return new WaitUntil(() => _canProceed);
        _canProceed = false;

        // チュートリアルで選択されなかったプレイヤーの選択データを自動で設定
        _selectTurn.FinalizeSelectionsForTutorial();

        OnPhaseFinished?.Invoke();
    }

    private void InitializeMessages()
    {
        _tutorialMessages = new Queue<string>();
        _tutorialMessages.Enqueue("このフェーズでは、各キャラクターが攻撃する敵の優先順位を決めます。（クリックで次へ）");
        _tutorialMessages.Enqueue("攻撃したい敵を選択し、Enterキーで決定します。\nこれをキャラクターの人数分、優先順位の数だけ繰り返します。（クリックで次へ）");
        _tutorialMessages.Enqueue("まず、最初のキャラクターの第1優先ターゲットを選択してみましょう。\n矢印キーで敵を選び、Enterキーで決定してください。");
        _tutorialMessages.Enqueue("うまく選択できましたね！\n実際のゲームでは、これを全キャラクター分行います。\n（クリックで次へ）");
    }

    private void SetTutorialText(string text)
    {
        _tutorialText.text = text;
    }
}
#endif
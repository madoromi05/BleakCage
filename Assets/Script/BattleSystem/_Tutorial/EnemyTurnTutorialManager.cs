using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if TUTORIAL_ENABLED

/// <summary>
/// 敵ターンのチュートリアル担当するクラス
/// </summary>
public class EnemyTurnTutorialManager : MonoBehaviour, IPhase
{
    public event System.Action OnPhaseFinished;

    [Header("Component References")]
    [SerializeField] public GameObject TutorialUIPanel;
    [SerializeField] private Text _tutorialText;
    [SerializeField] private EnemyTurn _enemyTurn;

    private TutorialInputReader _inputReader;
    private Queue<string> _enemyTurnMessages;
    private bool _canProceed = false;
    private bool _hasEnemyTurnFinished = false;

    public void Initialize(TutorialInputReader ir)
    {
        _inputReader = ir;
        if (_inputReader != null)
        {
            _inputReader.OnProceed += HandleProceedInput;
        }

        if (_enemyTurn != null)
        {
            _enemyTurn.TurnFinished += HandleEnemyTurnFinished;
        }
    }

    private void OnDisable()
    {
        if (_inputReader != null)
        {
            _inputReader.OnProceed -= HandleProceedInput;
        }
        if (_enemyTurn != null)
        {
            _enemyTurn.TurnFinished -= HandleEnemyTurnFinished;
        }
    }

    private void HandleProceedInput()
    {
        _canProceed = true;
    }

    private void HandleEnemyTurnFinished()
    {
        _hasEnemyTurnFinished = true;
    }

    public void StartPhase()
    {
        StartCoroutine(TutorialCoroutine());
    }

    /// <summary>
    /// チュートリアルの文章を順に表示し、敵ターンを実行するコルーチン 
    /// </summary>
    /// <returns></returns>
    private IEnumerator TutorialCoroutine()
    {
        InitializeEnemyTurnMessages();
        TutorialUIPanel.SetActive(true);
        _hasEnemyTurnFinished = false;
        _canProceed = false;
        yield return null;

        SetTutorialText(_enemyTurnMessages.Dequeue());
        yield return new WaitUntil(() => _canProceed);
        _canProceed = false;

        SetTutorialText(_enemyTurnMessages.Dequeue());
        yield return new WaitUntil(() => _canProceed);
        _canProceed = false;

        SetTutorialText(_enemyTurnMessages.Dequeue());
        yield return new WaitUntil(() => _canProceed);
        _canProceed = false;

        SetTutorialText(_enemyTurnMessages.Dequeue());
        yield return new WaitUntil(() => _canProceed);
        _canProceed = false;

        TutorialUIPanel.SetActive(false);
        _enemyTurn.StartEnemyTurn();

        // 敵ターンの実行が完了するのを待つ
        yield return new WaitUntil(() => _hasEnemyTurnFinished);

        // --- チュートリアル終了 ---
        yield return StartCoroutine(EndTutorial());
        OnPhaseFinished?.Invoke();
    }

    private void InitializeEnemyTurnMessages()
    {
        _enemyTurnMessages = new Queue<string>();
        _enemyTurnMessages.Enqueue("次は敵のターンです。\n敵が攻撃対象を選択し、攻撃してきます。");
        _enemyTurnMessages.Enqueue("敵の攻撃がヒットする瞬間（ジャストタイミング）で防御キーを押すと「COUNTER」となり、エクストラターンを獲得できます。");
        _enemyTurnMessages.Enqueue("ジャストタイミングより早くても、キーを押し続けていれば「GUARD」となり、ダメージを軽減できます。");
        _enemyTurnMessages.Enqueue("敵が攻撃してきます！防御の準備を！\n（クリックで次のステップへ）");
    }

    private IEnumerator EndTutorial()
    {
        TutorialUIPanel.SetActive(true);
        _tutorialText.text = "敵のターンが終了しました。\nこれでチュートリアルは終わりです。\n\n（クリックで通常の戦闘を開始します）";

        yield return new WaitUntil(() => _canProceed);
        _canProceed = false;

        TutorialUIPanel.SetActive(false);
        Debug.Log("敵ターンチュートリアル完了");
    }

    private void SetTutorialText(string text)
    {
        _tutorialText.text = text;
    }
}
#endif
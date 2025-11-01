using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if TUTORIAL_ENABLED

/// <summary>
/// 敵ターンのチュートリアル（防御/カウンター）を担当するクラス
/// </summary>
public class EnemyTurnTutorialManager : MonoBehaviour, IPhase
{
    public event System.Action OnPhaseFinished;

    [Header("Component References")]
    [SerializeField] public GameObject tutorialUIPanel;
    [SerializeField] private Text tutorialText;
    [SerializeField] private EnemyTurn enemyTurn;
    [SerializeField] private TutorialInputReader inputReader;

    private Queue<string> enemyTurnMessages;
    private bool canProceed = false;
    private bool hasEnemyTurnFinished = false;

    /// <summary>
    /// BattleManagerから呼び出される初期化
    /// </summary>
    public void Initialize()
    {
        if (inputReader != null)
        {
            inputReader.OnProceed += HandleProceedInput;
        }

        if (enemyTurn != null)
        {
            enemyTurn.TurnFinished += HandleEnemyTurnFinished;
        }
    }

    private void OnDisable()
    {
        if (inputReader != null)
        {
            inputReader.OnProceed -= HandleProceedInput;
        }
        if (enemyTurn != null)
        {
            enemyTurn.TurnFinished -= HandleEnemyTurnFinished;
        }
    }

    private void HandleProceedInput()
    {
        canProceed = true;
    }

    private void HandleEnemyTurnFinished()
    {
        hasEnemyTurnFinished = true;
    }

    /// <summary>
    /// IPhase インターフェース経由で BattleManager から呼び出される
    /// </summary>
    public void StartPhase()
    {
        StartCoroutine(TutorialCoroutine());
    }

    private IEnumerator TutorialCoroutine()
    {
        InitializeEnemyTurnMessages();
        tutorialUIPanel.SetActive(true);
        hasEnemyTurnFinished = false;
        canProceed = false;
        yield return null; // 入力食い込み防止

        // 1. メッセージ
        SetTutorialText(enemyTurnMessages.Dequeue());
        yield return new WaitUntil(() => canProceed);
        canProceed = false;
        yield return null;

        // 2. メッセージ
        SetTutorialText(enemyTurnMessages.Dequeue());
        yield return new WaitUntil(() => canProceed);
        canProceed = false;
        yield return null;

        // 3. メッセージ
        SetTutorialText(enemyTurnMessages.Dequeue());
        yield return new WaitUntil(() => canProceed);
        canProceed = false;
        yield return null;

        // 4. メッセージ (攻撃開始)
        SetTutorialText(enemyTurnMessages.Dequeue());
        yield return new WaitUntil(() => canProceed);
        canProceed = false;

        // UIを非表示にして、敵ターンを開始
        tutorialUIPanel.SetActive(false);
        enemyTurn.StartEnemyTurn();

        // 敵ターンの実行が完了するのを待つ
        yield return new WaitUntil(() => hasEnemyTurnFinished);

        // --- チュートリアル終了 ---
        yield return StartCoroutine(EndTutorial());

        // すべて完了したら、BattleManager に通知
        OnPhaseFinished?.Invoke();
    }

    private void InitializeEnemyTurnMessages()
    {
        enemyTurnMessages = new Queue<string>();
        enemyTurnMessages.Enqueue("次は敵のターンです。\n敵が攻撃対象を選択し、攻撃してきます。");
        enemyTurnMessages.Enqueue("敵の攻撃がヒットする瞬間（ジャストタイミング）で防御キーを押すと「COUNTER」となり、エクストラターンを獲得できます。");
        enemyTurnMessages.Enqueue("ジャストタイミングより早くても、キーを押し続けていれば「GUARD」となり、ダメージを軽減できます。");
        enemyTurnMessages.Enqueue("敵が攻撃してきます！防御の準備を！\n（今回はジャストガードを狙ってみましょう）");
    }

    private IEnumerator EndTutorial()
    {
        // 終了UIを表示
        tutorialUIPanel.SetActive(true);
        tutorialText.text = "敵のターンが終了しました。\nこれでチュートリアルは終わりです。\n\n（クリックで通常の戦闘を開始します）";

        yield return new WaitUntil(() => canProceed);
        canProceed = false;

        tutorialUIPanel.SetActive(false);
        Debug.Log("チュートリアル完了");
    }

    private void SetTutorialText(string text)
    {
        tutorialText.text = text;
    }
}
#endif
#if TUTORIAL_ENABLED
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectTurnTutorialManager : MonoBehaviour, IPhase
{
    [SerializeField] private GameObject tutorialUIPanel;
    [SerializeField] private TextMeshProUGUI tutorialText;
    [SerializeField] private RawImage tutorialGifImage;
    [SerializeField] private GifViewController gifView;
    [SerializeField] private SelectTurn selectTurn;

    private TutorialInputReader _inputReader;
    private List<PlayerRuntime> _currentParty;
    private List<EnemyModel> _currentEnemies;
    private List<PlayerStatusUIController> _playerUIs;
    private List<EnemyStatusUIController> _enemyUIs;

    private Queue<string> tutorialMessages;
    private bool canProceed = false;

    public event System.Action OnSelectTurnTutorialFinished;
    public event System.Action OnPhaseFinished;

    public void Initialize(TutorialInputReader ir, List<PlayerRuntime> players, List<EnemyModel> enemies, List<PlayerStatusUIController> pUIs, List<EnemyStatusUIController> eUIs)
    {
        _inputReader = ir;

        // 自身のメンバ変数にリストを保存する
        _currentParty = players;
        _currentEnemies = enemies;
        _playerUIs = pUIs;
        _enemyUIs = eUIs;

        // SelectTurn の Initialize を呼び出す
        selectTurn.Initialize(players, enemies, pUIs, eUIs);
    }

    public void StartPhase()
    {
        Debug.Log("Starting Select Turn Tutorial Phase");
        if (_inputReader != null)
        {
            _inputReader.OnProceed += HandleProceedInput;
            Debug.Log("InputReaderNOTNULL");
        }
        Debug.Log("InputReaderisOK");

        gifView.Initialize(tutorialGifImage);
        tutorialUIPanel.SetActive(true);
        StartCoroutine(TutorialCoroutine());
    }

    private void HandleProceedInput()
    {
        canProceed = true;
        Debug.Log("HandleProceedInput is Call");
    }


    private IEnumerator TutorialCoroutine()
    {
        InitializeMessages();

        // 1. 最初のメッセージ
        SetTutorialText(tutorialMessages.Dequeue()); // "このフェーズでは..."
        yield return new WaitUntil(() => canProceed);
        canProceed = false;

        // 2. GIF付きの説明
        SetTutorialTextAndGif(tutorialMessages.Dequeue(), "select_target.gif"); // "<gif>矢印キーで..."
        yield return new WaitUntil(() => canProceed);
        canProceed = false;
        gifView.StopGif();

        // 3. 実際の選択を促す
        SetTutorialText(tutorialMessages.Dequeue()); // "まず、最初のキャラクターの..."

        // チュートリアルで操作するプレイヤー（1人目）とそのUIを取得
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
        yield return StartCoroutine(selectTurn.SelectOneTargetCoroutine(tutorialPlayer, 1, (selectedEnemy) => {
            Debug.Log($"Tutorial selection complete: {selectedEnemy.EnemyName}");
        }));

        // プレイヤーUIのハイライトを戻す
        tutorialPlayerUI.ResetHighlight();


        // 4. 完了メッセージ
        SetTutorialText(tutorialMessages.Dequeue()); // "うまく選択できましたね！..."
        yield return new WaitUntil(() => canProceed);
        canProceed = false;

         // selectTurn.FinalizeSelectionsForTutorial();

        OnPhaseFinished?.Invoke();
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
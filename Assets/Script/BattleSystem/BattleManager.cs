using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;

/// <summary>
/// Battleの流れを管理するクラス
/// </summary>
public class BattleManager : MonoBehaviour
{
    public TextMeshProUGUI timeText;

    [SerializeField] private PlayerTurn playerTurn;
    [SerializeField] private EnemyTurn enemyTurn;
    [SerializeField] private SelectTurn selectTurn;
    [SerializeField] private BattleCardDeck battleDeck;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform partyTextureTransform;
    [SerializeField] private Transform enemyTextureTransform;
    [SerializeField] private GameObject Line;

    private PlayerModelFactory playerModelFactory = new PlayerModelFactory();
    private List<PlayerRuntime> party = new List<PlayerRuntime>();
    private List<EnemyModel> predators = new List<EnemyModel>();
    private List<PlayerStatusUIController> playerUIs = new List<PlayerStatusUIController>();
    private List<EnemyStatusUIController> enemyUIs = new List<EnemyStatusUIController>();

    private EnemyModel enemyModel;
    private float turnTime = 10f; // プレイヤーのターン時間（秒）
    private bool isSelectionPhase = false;

    private bool SelectTask = false;
    private int TurnCount;
    private List<EnemyModel> Player1Select = new List<EnemyModel>();
    private List<EnemyModel> Player2Select = new List<EnemyModel>();
    private List<EnemyModel> Player3Select = new List<EnemyModel>();

    public event System.Action BattleFinished;                    // バトル終了イベント
    void Start()
    {
        Instantiate(Line);
        
        TurnCount = 1;
        // 1. パーティーとカードデータを読み込み
        var dataLoader = new PlayerDataLoader();
        DeckSetupRepository setupData = dataLoader.LoadPlayerPartyAndCards();
        this.party = setupData.Party;

        // 2. パーティ人数分 PlayerView を生成
        partyPlayerView();

        // 3. 敵を生成(ID一から3まで)
        enemiesCreate();

        //最初の敵をターゲットとして設定する
        if (predators.Count > 0)
        {
            enemyModel = predators[0];
        }

        List<PlayerModel> playerModels = party.Select(p => p.PlayerModel).ToList();
        enemyTurn.EnemySetup(playerModels, predators);

        // 4. バトルデッキとプレイヤーのターンをセットアップ
        battleDeck.InitFromCardList(setupData.AllCards);
        playerTurn.TurnFinished += OnPlayerTurnFinished;

        enemyTurn.EnemyTurnFinished += OnEnemyTurnFinished;
        selectTurn.SelectTurnFinished += OnSelectTurnFinished;

        playerTurn.CheckDead += PredatorDeadOrDead;
        BattleFinished += DieEnemy;
        // 5. バトル開始
        StartCoroutine(BattleFlow());
    }

    private void partyPlayerView()
    {
        for (int i = 0; i < party.Count; i++)
        {
            PlayerRuntime runtime = party[i];

            var playerObject = Instantiate(playerPrefab, partyTextureTransform, false);

            PlayerController playerController = playerObject.GetComponent<PlayerController>();
            playerController.Init(runtime.PlayerModel);
        }
    }

    private void enemiesCreate()
    {
        var enemyFactory = new EnemyModelFactory();
        for (int i = 0; i < 3; i++)
        {
            EnemyModel enemy = enemyFactory.CreateFromId(i + 1);
            predators.Add(enemy);

            var enemyObject = Instantiate(enemyPrefab, enemyTextureTransform, false);
            EnemyController enemyController = enemyObject.GetComponent<EnemyController>();
            enemyController.Init(enemy);
        }
    }

    private IEnumerator BattleFlow()
    {
        while (true) // ゲーム終了までループ
        {
            // 1. 選択ターン
            isSelectionPhase = true;
            Debug.Log($"【ターン{TurnCount}: 攻撃優先順位選択 開始】");
            yield return StartCoroutine(SelectionProcessCoroutine());
            // OnSelectTurnFinishedで isSelectionPhase が false になるのを待つ
            yield return new WaitUntil(() => !isSelectionPhase);
            Debug.Log("【攻撃優先順位選択 終了】");

            // 2. プレイヤーの攻撃ターン
            Debug.Log("【プレイヤーターン開始】");
            playerTurn.StartPlayerTurn();
            turnTime = 10f;
            while (turnTime > 0)
            {
                if (isSelectionPhase) yield break; // 選択フェーズに戻ったら中断
                turnTime -= Time.deltaTime;
                timeText.text = turnTime.ToString("f2") + " <size=70%>SECOND</size>";
                yield return null;
            }
            playerTurn.FinishPlayerTurn(); // 時間切れでターン終了

            // 3. 敵のターン
            // OnPlayerTurnFinished -> EnemyTurn -> OnEnemyTurnFinished の流れは既存のイベントで処理される

            // 4. 次のターンへ
            TurnCount++;
        }
    }

    /// <summary>
    /// Playerターン開始時の処理
    /// </summary>
    private void StartPlayerTurn()
    {
        SelectTask = false;
        turnTime = 10f;
        StartCoroutine(StartPlayerTurnWithTimer());
    }

    /// <summary>
    /// プレイヤーのターンを10秒間開始
    /// </summary>
    private IEnumerator StartPlayerTurnWithTimer()
    {
        yield return new WaitUntil(() => SelectTask);
        Debug.Log("【プレイヤーターン開始】");
        playerTurn.StartPlayerTurn();

        while (turnTime >= 0)
        {
            turnTime -= Time.deltaTime;
            timeText.text = turnTime.ToString("f2") + " <size=70%>SECOND</size>";
            yield return null;
        }
        playerTurn.FinishPlayerTurn();
    }

    /// <summary>
    /// プレイヤーターン終了 → 敵ターン → プレイヤーターン再開
    /// </summary>
    private void OnPlayerTurnFinished()
    {
        Debug.Log("【プレイヤーターン終了】");
        EnemyTurn();
    }

    /// <summary>
    /// 敵のターン（仮実装）→ すぐにプレイヤーターン再開
    /// </summary>
    private void EnemyTurn()
    {
        Debug.Log("【敵ターン開始】");
        enemyTurn.StartEnemyTurn();
    }

    /// <summary>
    /// プレイヤーターン終了 → 敵ターン → プレイヤーターン再開
    /// </summary>
    private void OnEnemyTurnFinished()
    {
        Debug.Log("【敵ターン終了】"); 
        SelectTask = false;
        TurnCount++;
        StartCoroutine(SelectTurnSkip());
        StartPlayerTurn();
    }

    /// <summary>
    /// 選択パス
    /// </summary>
    private IEnumerator SelectTurnSkip()
    {
        if (TurnCount > 1)
        {
            Debug.Log("選択をスキップしますか？Enterでスキップspaceで継続");

            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space));

            if (Input.GetKeyDown(KeyCode.Return))
            {
                StartPlayerTurn();
                yield break;
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                selectTurn.StartSelectTurn(party, predators);
                yield break;
            }
        }
        else
        {
            StartSelectTurn();
            yield break;
        }
    }

    /// <summary>
    /// 新しいキーボード入力による選択プロセス
    /// </summary>
    private IEnumerator SelectionProcessCoroutine()
    {
        selectTurn.StartSelectTurn(party, predators);

        // プレイヤー3人分の選択ループ
        for (int pIndex = 0; pIndex < party.Count; pIndex++)
        {
            if (pIndex >= playerUIs.Count) continue; // UIがない場合はスキップ
            playerUIs[pIndex].StartFlashing(Color.blue);

            // 優先順位3つ分の選択ループ
            for (int eIndex = 0; eIndex < predators.Count; eIndex++)
            {
                if (eIndex >= enemyUIs.Count) continue; // UIがない場合はスキップ
                enemyUIs[eIndex].StartFlashing(Color.red);

                Debug.Log($"Player {pIndex + 1} の 優先順位{eIndex + 1} を選択してください。対象: {predators[eIndex].EnemyName} (Enterキーで決定)");

                // Enterキーが押されるまで待機
                yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Return));

                enemyUIs[eIndex].StopFlashing();

                // 選択を登録
                // selectTurn.RegisterSelection(party[pIndex].PlayerModel, predators[eIndex]);
            }
            playerUIs[pIndex].StopFlashing();
        }
    }

    /// <summary>
    /// 選択開始
    /// </summary>
    private void StartSelectTurn()
    {

        Debug.Log("選択ターン開始");
        selectTurn.StartSelectTurn(party, predators);
    }
    private void OnSelectTurnFinished()
    {
        var selections = selectTurn.PlayerSelections;

        //バトルデッキとプレイヤーのターンをセットアップ
        for (int i = 0; i < 3; i++)
        {
            if (Player1Select[i] != null)
            {
                playerTurn.Setup(party[0], Player1Select[i], battleDeck);
                break;
            }
        }
        for (int i = 0; i < 3; i++)
        {
            if (Player2Select[i] != null)
            {
                playerTurn.Setup(party[1], Player2Select[i], battleDeck);
                break;
            }
        }
        for (int i = 0; i < 3; i++)
        {
            if (Player3Select[i] != null)
            {
                playerTurn.Setup(party[2], Player3Select[i], battleDeck);
                break;
            }
        }

        for (int i = 0; i < party.Count; i++)
        {
            List<EnemyModel> targetList = null;
            if (i == 0) targetList = Player1Select;
            if (i == 1) targetList = Player2Select;
            if (i == 2) targetList = Player3Select;

            // 死んでいない最初の敵をターゲットにする
            EnemyModel firstAliveTarget = targetList.FirstOrDefault(e => e.EnemyHP > 0);
            if (firstAliveTarget != null)
            {
                playerTurn.Setup(party[i], firstAliveTarget, battleDeck);
            }
        }
        Debug.Log("選択ターン終了");
        SelectTask = true;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void PredatorDeadOrDead()
    {
        if(predators == null)
        {
            BattleFinished?.Invoke();
        }
    }
    private void DieEnemy()
    {
        Debug.Log("敵艦隊の消滅を確認！帰投します");
    }
}
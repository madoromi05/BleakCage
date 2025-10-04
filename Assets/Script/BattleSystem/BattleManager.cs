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

    private EnemyModel enemyModel;
    private float turnTime = 10f; // プレイヤーのターン時間（秒）

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
        StartSelectTurn();
        StartPlayerTurn();

        PredatorDeadOrDead();
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
        if(TurnCount > 1)
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
    /// 選択開始
    /// </summary>
    private void StartSelectTurn()
    {

        Debug.Log("選択ターン開始");
        selectTurn.StartSelectTurn(party, predators);
    }
    private void OnSelectTurnFinished()
    {
        Player1Select = selectTurn.SelectPlayer1;
        Player2Select = selectTurn.SelectPlayer2;
        Player3Select = selectTurn.SelectPlayer3;
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
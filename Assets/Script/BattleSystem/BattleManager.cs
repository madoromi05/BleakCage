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
    [SerializeField] private BattleCardDeck battleDeck;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform partyTextureTransform;
    [SerializeField] private Transform enemyTextureTransform;

    private PlayerModelFactory playerModelFactory = new PlayerModelFactory();
    private List<PlayerRuntime> party = new List<PlayerRuntime>();
    private List<EnemyModel> predators = new List<EnemyModel>();

    private EnemyModel enemyModel;
    private float turnTime = 10f; // プレイヤーのターン時間（秒）

    void Start()
    {
        // 1. PlayerDataLoaderを使ってパーティーとカードデータを読み込む
        var dataLoader = new PlayerDataLoader();
        DeckSetupRepository setupData = dataLoader.LoadPlayerPartyAndCards();

        // 2. 読み込んだPlayerデータをBattleManagerに設定
        this.party = setupData.Party;

        // 2. パーティ人数分 PlayerView を生成
        for (int i = 0; i < party.Count; i++)
        {
            PlayerRuntime runtime = party[i];

            var playerObject = Instantiate(playerPrefab, partyTextureTransform, false);

            PlayerController playerController = playerObject.GetComponent<PlayerController>();
            playerController.Init(runtime.PlayerModel);
        }

        // 3. 敵を生成(ID一から3まで)
        var enemyFactory = new EnemyModelFactory();
        for (int i = 0; i < 3; i++)
        {
            EnemyModel enemy = enemyFactory.CreateFromId(i + 1);
            predators.Add(enemy);

            var enemyObject = Instantiate(enemyPrefab, enemyTextureTransform, false);
            EnemyController enemyController = enemyObject.GetComponent<EnemyController>();
            enemyController.Init(enemy);
        }

        //最初の敵をターゲットとして設定する
        if (predators.Count > 0)
        {
            enemyModel = predators[0];
        }
        else
        {
            Debug.LogError("攻撃対象の敵が見つかりません！");
            return;
        }

        List<PlayerModel> playerModels = party.Select(p => p.PlayerModel).ToList();
        enemyTurn.EnemySetup(playerModels, predators);

        // 4. バトルデッキとプレイヤーのターンをセットアップ
        battleDeck.InitFromCardList(setupData.AllCards);
        playerTurn.Setup(party[0], enemyModel, battleDeck);
        playerTurn.TurnFinished += OnPlayerTurnFinished;

        // 5. バトル開始
        StartPlayerTurn();
    }

    /// <summary>
    /// Playerターン開始時の処理
    /// </summary>
    private void StartPlayerTurn()
    {
        turnTime = 10f;
        StartCoroutine(StartPlayerTurnWithTimer());
    }

    /// <summary>
    /// プレイヤーのターンを10秒間開始
    /// </summary>
    private IEnumerator StartPlayerTurnWithTimer()
    {
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
        StartCoroutine(EnemyTurn());
    }

    /// <summary>
    /// 敵のターン（仮実装）→ すぐにプレイヤーターン再開
    /// </summary>
    private IEnumerator EnemyTurn()
    {
        Debug.Log("【敵ターン開始】");
        enemyTurn.StartEnemyTurn();

        yield return new WaitForSeconds(1.0f);

        Debug.Log("【敵ターン終了】");

        StartPlayerTurn();
    }
}
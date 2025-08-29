using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Battleの流れを管理するクラス
/// </summary>
public class BattleManager : MonoBehaviour
{
    public TextMeshProUGUI timeText;

    [SerializeField] private PlayerTurn playerTurn;
    [SerializeField] private BattleCardDeck battleDeck;

    private List<PlayerRuntime> party; // = new List<PlayerRuntime>(); // 初期化はStartで行う
    private EnemyModel enemyModel;
    private float turnTime = 10f; // プレイヤーのターン時間（秒）

    void Start()
    {
        // 1. PlayerDataLoaderを使ってパーティーとカードデータを読み込む
        var dataLoader = new PlayerDataLoader();
        DeckSetupRepository setupData = dataLoader.LoadPlayerPartyAndCards();

        // 2. 読み込んだデータをBattleManagerに設定
        this.party = setupData.Party;

        // 3. 敵を生成
        var enemyFactory = new EnemyModelFactory();
        int mockEnemyId = 1;
        enemyModel = enemyFactory.CreateFromId(mockEnemyId);

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

        yield return new WaitForSeconds(1.0f);

        Debug.Log("【敵ターン終了】");

        StartPlayerTurn();
    }
}
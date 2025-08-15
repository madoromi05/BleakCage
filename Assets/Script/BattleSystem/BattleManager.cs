using System.Collections;
using System.Drawing;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
/// <summary>
/// Battleの流れを管理するクラス 
/// </summary>
public class BattleManager : MonoBehaviour
{
    public TextMeshProUGUI timeText;                        //時間を表示する変数

    [SerializeField] private PlayerTurn playerTurn;
    [SerializeField] private BattleCardDeck battleDeck;
    [SerializeField] private PlayerCardDeck playerDeck;
    private EnemyModel enemyModel;
    private EnemyModelFactory enemyFactory;
    private PlayerModel playerModel;
    private PlayerRuntime playerRuntime;
    private PlayerModelFactory playerFactory;
    private WeaponModel weaponModel;
    private WeaponRuntime weaponRuntime;
    private WeaponModelFactory weaponFactory;
    private CardModelFactory cardFactory;
    private float turnTime = 10f;                           // プレイヤーのターン時間（秒）

    void Start()
    {
        playerFactory = new PlayerModelFactory();
        enemyFactory  = new EnemyModelFactory();
        weaponFactory = new WeaponModelFactory();
        cardFactory   = new CardModelFactory();

        // @Demoサーバーからデータ取得してIDを得たと仮定
        int mockPlayerId = 1;
        int mockEnemyId  = 1;
        int mockWeaponId = 1;

        playerModel = playerFactory.CreateFromId(mockPlayerId);
        enemyModel = enemyFactory.CreateFromId(mockEnemyId);
        weaponModel = new WeaponModelFactory().CreateFromId(mockWeaponId);

        IAttackStrategy defaultStrategy = new AttributeWeakness();
        playerRuntime = new PlayerRuntime(playerModel, defaultStrategy);
        weaponRuntime = new WeaponRuntime(weaponModel);

        // プレイヤーに武器を装備させる
        playerRuntime.EquipWeapon(weaponRuntime);

        // PlayerDeck生成
        playerTurn.Setup(playerRuntime, weaponRuntime, enemyModel, playerDeck, battleDeck);
        playerTurn.TurnFinished += OnPlayerTurnFinished;
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

        playerTurn.FinishPlayerTurn(); // 強制終了（手動でも終了可能）
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

        // ここに敵の行動処理を書く（今は0.5秒待機のダミー）
        yield return new WaitForSeconds(1.0f);

        Debug.Log("【敵ターン終了】");

        // 次のプレイヤーターン開始
        StartPlayerTurn();
    }
}
